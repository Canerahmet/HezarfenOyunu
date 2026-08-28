using System;
using Hezarfen.Flight;
using UnityEngine;

namespace Hezarfen.Player
{
    /// <summary>
    /// <b>Kule tepesinde kuşanma → atlayış → süzülüş → iniş.</b>
    ///
    /// Faz 5'in kabul ölçütü bu zincirin **kesintisiz** olmasıdır. Zincir
    /// iki ayrı fizik dünyasını birleştirir ve asıl iş orada:
    ///
    /// - Yerde <see cref="CharacterController"/> sürer — kapsül, adım
    ///   yüksekliği, eğim sınırı. Kütlesi yoktur, ivmesi yoktur, çarpınca
    ///   durur.
    /// - Havada <see cref="Rigidbody"/> sürer — kütle, sürükleme,
    ///   kaldırma. Kanat bir kuvvet uygular, karakter ona uyar.
    ///
    /// İkisi aynı anda açık olamaz: `CharacterController` her karede
    /// konumu kendisi yazar ve Rigidbody'nin uyguladığı her kuvveti
    /// sessizce yutar. Bu, "uçuyorum ama düşmüyorum" diye görünen türden
    /// bir hatadır.
    ///
    /// ## Neden bir durum makinesi, neden bayrak değil
    ///
    /// "Uçuyor mu" diye tek bir bool tutmak yetmez, çünkü aradaki
    /// **geçişlerin süresi var**: kuşanma iki buçuk saniye sürer ve o
    /// sırada ne yürünür ne uçulur. Bayrakla yazınca o iki buçuk saniye
    /// ya yok sayılır (animasyon kesilir) ya da oyuncu donar (girdi
    /// kaybolur). Durum makinesi süreyi bir yer olarak tutar.
    /// </summary>
    [DisallowMultipleComponent]
    public class UcusDizisi : MonoBehaviour
    {
        public enum Durum
        {
            /// <summary>Yerde, kanat sırtta katlı.</summary>
            Yerde,
            /// <summary>Kanat kuşanılıyor — girdi kilitli, klip oynuyor.</summary>
            Kusaniyor,
            /// <summary>Kanat açık, kenarda bekliyor. Atlayabilir.</summary>
            Hazir,
            /// <summary>Havada.</summary>
            Ucuyor,
            /// <summary>Yere değdi, iniş klibi oynuyor.</summary>
            Iniyor,
            /// <summary>Sert çarptı.</summary>
            Cakildi,
        }

        [Header("Bağlantılar")]
        public HezarfenAnimator animasyon;
        public WalkController yurume;
        public CharacterController kapsul;
        public Rigidbody govde;
        public GlideController suzulme;
        public FlightLaunch firlatma;

        [Header("Süreler (s) — klip uzunluklarıyla aynı olmalı")]
        [Tooltip("Kuşanma klibinin uzunluğu. Kısa verilirse oyuncu " +
                 "animasyon bitmeden atlar ve kanat sırtta görünürken uçar.")]
        public float kusanmaSuresi = 2.5f;

        [Tooltip("İniş/yuvarlanma klibinin uzunluğu.")]
        public float inisSuresi = 1.5f;

        [Header("İniş ölçütü")]
        [Tooltip("Yere değerken bu dikey hızın altındaysa iniş, " +
                 "üstündeyse çakılma (m/s, negatif = aşağı).")]
        public float cakilmaHizi = -9f;

        [Tooltip("Zeminden bu kadar yakınsa 'yere değdi' sayılır (m).")]
        public float temasMesafesi = 0.35f;

        [Header("Girdi")]
        public KeyCode kusanTusu = KeyCode.E;
        public KeyCode atlaTusu = KeyCode.Space;

        /// <summary>Şu anki durum — HUD ve test okur.</summary>
        public Durum Simdiki { get; private set; } = Durum.Yerde;

        /// <summary>Durum değişince tetiklenir (HUD, ses, kodeks).</summary>
        public event Action<Durum> DurumDegisti;

        private float _sayac;

        private void Awake()
        {
            if (animasyon == null) animasyon = GetComponent<HezarfenAnimator>();
            if (yurume == null) yurume = GetComponent<WalkController>();
            if (kapsul == null) kapsul = GetComponent<CharacterController>();
            if (govde == null) govde = GetComponent<Rigidbody>();
            if (suzulme == null) suzulme = GetComponent<GlideController>();
            if (firlatma == null) firlatma = GetComponent<FlightLaunch>();
            YereGec();
        }

        private void Update()
        {
            switch (Simdiki)
            {
                case Durum.Yerde:
                    if (Input.GetKeyDown(kusanTusu)) Kusan();
                    break;

                case Durum.Kusaniyor:
                    _sayac -= Time.deltaTime;
                    if (_sayac <= 0f) Gec(Durum.Hazir);
                    break;

                case Durum.Hazir:
                    if (Input.GetKeyDown(atlaTusu)) Atla();
                    break;

                case Durum.Ucuyor:
                    TemasDenetle();
                    break;

                case Durum.Iniyor:
                case Durum.Cakildi:
                    _sayac -= Time.deltaTime;
                    if (_sayac <= 0f) { YereGec(); Gec(Durum.Yerde); }
                    break;
            }
        }

        /// <summary>Kanadı kuşan — girdi bu süre boyunca kilitli.</summary>
        public void Kusan()
        {
            if (Simdiki != Durum.Yerde) return;
            animasyon?.Kusan();
            _sayac = kusanmaSuresi;
            Gec(Durum.Kusaniyor);
        }

        /// <summary>Atla: yürüme fiziği kapanır, uçuş fiziği açılır.</summary>
        public void Atla()
        {
            if (Simdiki != Durum.Hazir) return;
            animasyon?.Atla();
            HavayaGec();
            Gec(Durum.Ucuyor);
        }

        private void TemasDenetle()
        {
            // Zeminle temas: ışın gövdenin ALTINDAN atılır, merkezden
            // değil — merkezden atılan ışın karakterin kendi
            // çarpıştırıcısına takılır.
            Vector3 bas = transform.position + Vector3.up * 0.2f;
            if (!Physics.Raycast(bas, Vector3.down,
                                 temasMesafesi + 0.2f,
                                 ~0, QueryTriggerInteraction.Ignore))
                return;

            float dikey = govde != null ? govde.linearVelocity.y : 0f;
            bool sert = dikey < cakilmaHizi;
            if (sert) { animasyon?.Cakil(); _sayac = 1.0f; Gec(Durum.Cakildi); }
            else { animasyon?.In(); _sayac = inisSuresi; Gec(Durum.Iniyor); }

            // Fizik HEMEN kapanir; animasyon suresi boyunca karakter
            // yerdedir. Beklemek, inis klibi oynarken govdenin
            // yuvarlanmaya devam etmesi demekti.
            YereGec();
        }

        private void HavayaGec()
        {
            // SIRA ONEMLI: kapsul once kapanmali. Acikken Rigidbody'yi
            // kinematik olmaktan cikarmak, ayni karede iki farkli
            // konum yazicisi demek.
            if (yurume != null) yurume.enabled = false;
            if (kapsul != null) kapsul.enabled = false;
            if (govde != null)
            {
                govde.isKinematic = false;
                govde.useGravity = true;
            }
            if (suzulme != null) suzulme.enabled = true;
            if (firlatma != null) firlatma.Launch();
        }

        private void YereGec()
        {
            if (suzulme != null) suzulme.enabled = false;
            if (govde != null)
            {
                govde.linearVelocity = Vector3.zero;
                govde.angularVelocity = Vector3.zero;
                govde.isKinematic = true;
                govde.useGravity = false;
            }
            if (kapsul != null) kapsul.enabled = true;
            if (yurume != null) yurume.enabled = true;
        }

        private void Gec(Durum yeni)
        {
            if (Simdiki == yeni) return;
            Simdiki = yeni;
            DurumDegisti?.Invoke(yeni);
        }
    }
}
