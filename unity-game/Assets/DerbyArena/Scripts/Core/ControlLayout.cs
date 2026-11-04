using UnityEngine;

namespace DerbyArena.Core
{
    /// <summary>The on-screen controls a player can move and resize (PUBG Mobile style layout editor).</summary>
    public enum ControlId { Steer = 0, Gas = 1, Brake = 2, Fire = 3, Rocket = 4, Nitro = 5, Drift = 6 }

    /// <summary>How the car is steered on touch screens.</summary>
    public enum SteerMode { Wheel = 0, Arrows = 1, Swipe = 2, Tilt = 3 }

    /// <summary>
    /// Control layout model (4.0.1). Every movable control has a normalised position inside the safe area (0..1,
    /// pivot centre) and its own scale; the whole set has a global size and opacity. Three presets ship (Classic,
    /// Driver, Left-handed) and anything the player drags becomes the Custom layout. Values live in SaveData
    /// (ControlPos = x, y, scale per control, in <see cref="ControlId"/> order) and are sanitised on load, so a save
    /// from an older version simply gets the preset.
    /// </summary>
    public static class ControlLayout
    {
        public const int Count = 7;
        public const int Classic = 0, Driver = 1, LeftHand = 2, Custom = 3;
        public const float MinScale = 0.6f, MaxScale = 1.6f;

        // base sizes in reference pixels (1920 x 1080, height-matched canvas)
        public static Vector2 BaseSize(ControlId id, SteerMode mode)
        {
            switch (id)
            {
                case ControlId.Steer:
                    switch (mode)
                    {
                        case SteerMode.Arrows: return new Vector2(430f, 200f);
                        case SteerMode.Swipe: return new Vector2(260f, 260f);
                        case SteerMode.Tilt: return new Vector2(220f, 220f);
                        default: return new Vector2(360f, 360f);
                    }
                case ControlId.Gas: return new Vector2(190f, 270f);
                case ControlId.Brake: return new Vector2(170f, 190f);
                case ControlId.Fire: return new Vector2(230f, 230f);
                case ControlId.Rocket: return new Vector2(150f, 150f);
                case ControlId.Nitro: return new Vector2(140f, 140f);
                default: return new Vector2(125f, 125f);
            }
        }

        /// <summary>Fills <paramref name="dst"/> (3 floats per control) with a preset; Custom keeps what is there.</summary>
        public static void Preset(int preset, float[] dst)
        {
            if (dst == null || dst.Length < Count * 3) return;
            float[,] p;
            switch (preset)
            {
                case Driver:
                    p = new float[,] {
                        { 0.115f, 0.20f }, { 0.90f, 0.15f }, { 0.76f, 0.11f }, { 0.885f, 0.56f },
                        { 0.745f, 0.47f }, { 0.095f, 0.56f }, { 0.30f, 0.13f } };
                    break;
                case LeftHand:
                    p = new float[,] {
                        { 0.885f, 0.19f }, { 0.115f, 0.147f }, { 0.255f, 0.109f }, { 0.14f, 0.58f },
                        { 0.29f, 0.50f }, { 0.385f, 0.27f }, { 0.38f, 0.10f } };
                    break;
                case Custom:
                    return;
                default:
                    p = new float[,] {
                        { 0.115f, 0.19f }, { 0.885f, 0.147f }, { 0.745f, 0.109f }, { 0.86f, 0.58f },
                        { 0.71f, 0.50f }, { 0.615f, 0.27f }, { 0.62f, 0.10f } };
                    break;
            }
            for (int i = 0; i < Count; i++)
            {
                dst[i * 3] = p[i, 0];
                dst[i * 3 + 1] = p[i, 1];
                dst[i * 3 + 2] = 1f;
            }
        }

        /// <summary>The saved layout, created from the saved preset when missing or malformed.</summary>
        public static float[] Current()
        {
            var d = Save.Data;
            if (d.ControlPos == null || d.ControlPos.Length != Count * 3)
            {
                d.ControlPos = new float[Count * 3];
                Preset(d.ControlPreset == Custom ? Classic : d.ControlPreset, d.ControlPos);
            }
            return d.ControlPos;
        }

        public static Vector2 Pos(ControlId id)
        {
            var l = Current();
            int i = (int)id * 3;
            return new Vector2(Mathf.Clamp01(l[i]), Mathf.Clamp01(l[i + 1]));
        }

        public static float Scale(ControlId id)
        {
            var l = Current();
            return Mathf.Clamp(l[(int)id * 3 + 2], MinScale, MaxScale);
        }

        public static void SetPos(ControlId id, Vector2 pos)
        {
            var l = Current();
            l[(int)id * 3] = Mathf.Clamp01(pos.x);
            l[(int)id * 3 + 1] = Mathf.Clamp01(pos.y);
            Save.Data.ControlPreset = Custom;
        }

        public static void SetScale(ControlId id, float scale)
        {
            var l = Current();
            l[(int)id * 3 + 2] = Mathf.Clamp(scale, MinScale, MaxScale);
            Save.Data.ControlPreset = Custom;
        }

        public static void ApplyPreset(int preset)
        {
            var l = Current();
            Preset(preset, l);
            Save.Data.ControlPreset = Mathf.Clamp(preset, Classic, Custom);
        }

        /// <summary>Resets positions, sizes, opacity and the steering options to the shipped defaults.</summary>
        public static void ResetAll()
        {
            var d = Save.Data;
            d.ControlPos = null;
            d.ControlPreset = Classic;
            d.ControlScale = 1f;
            d.ControlAlpha = 1f;
            d.ControlMode = (int)SteerMode.Swipe;
            d.Sensitivity = 1f;
            d.AutoGas = true;
            d.SteerAssist = true;
            Current();
        }

        public static void Sanitize(SaveData d)
        {
            if (d.ControlPos != null && d.ControlPos.Length != Count * 3) d.ControlPos = null;
            if (d.ControlPos != null)
            {
                for (int i = 0; i < Count; i++)
                {
                    d.ControlPos[i * 3] = Mathf.Clamp01(d.ControlPos[i * 3]);
                    d.ControlPos[i * 3 + 1] = Mathf.Clamp01(d.ControlPos[i * 3 + 1]);
                    float s = d.ControlPos[i * 3 + 2];
                    d.ControlPos[i * 3 + 2] = float.IsNaN(s) || s <= 0f ? 1f : Mathf.Clamp(s, MinScale, MaxScale);
                }
            }
            d.ControlPreset = Mathf.Clamp(d.ControlPreset, Classic, Custom);
            d.ControlScale = float.IsNaN(d.ControlScale) || d.ControlScale <= 0f ? 1f : Mathf.Clamp(d.ControlScale, 0.7f, 1.4f);
            d.ControlAlpha = float.IsNaN(d.ControlAlpha) || d.ControlAlpha <= 0f ? 1f : Mathf.Clamp(d.ControlAlpha, 0.4f, 1f);
            d.ControlMode = Mathf.Clamp(d.ControlMode, 0, 3);
        }

        public static SteerMode Mode { get { return (SteerMode)Mathf.Clamp(Save.Data.ControlMode, 0, 3); } }

        public static string Name(ControlId id)
        {
            switch (id)
            {
                case ControlId.Steer: return Loc.T("DİREKSİYON", "STEERING");
                case ControlId.Gas: return Loc.T("GAZ", "GAS");
                case ControlId.Brake: return Loc.T("FREN / GERİ", "BRAKE / REVERSE");
                case ControlId.Fire: return Loc.T("ATEŞ", "FIRE");
                case ControlId.Rocket: return Loc.T("ROKET / HE MERMİ", "ROCKET / HE SHELL");
                case ControlId.Nitro: return Loc.T("NİTRO", "NITRO");
                default: return Loc.T("DRİFT", "DRIFT");
            }
        }

        public static string PresetName(int preset)
        {
            switch (preset)
            {
                case Driver: return Loc.T("SÜRÜCÜ", "DRIVER");
                case LeftHand: return Loc.T("SOL EL", "LEFT HAND");
                case Custom: return Loc.T("ÖZEL", "CUSTOM");
                default: return Loc.T("KLASİK", "CLASSIC");
            }
        }

        public static string ModeName(SteerMode mode)
        {
            switch (mode)
            {
                case SteerMode.Wheel: return Loc.T("DİREKSİYON SİMİDİ", "STEERING WHEEL");
                case SteerMode.Arrows: return Loc.T("SAĞ / SOL OKLAR", "LEFT / RIGHT ARROWS");
                case SteerMode.Tilt: return Loc.T("EĞİM (TELEFONU YATIR)", "TILT (TURN THE PHONE)");
                default: return Loc.T("KAYDIRMA (JOYSTICK)", "SWIPE (JOYSTICK)");
            }
        }

        public static string ModeHint(SteerMode mode)
        {
            switch (mode)
            {
                case SteerMode.Wheel: return Loc.T("Simidi parmağınla çevir. Bırakınca düzelir.", "Turn the wheel with a finger. It centres when released.");
                case SteerMode.Arrows: return Loc.T("Sol ve sağ düğmelere bas.", "Hold the left and right buttons.");
                case SteerMode.Tilt: return Loc.T("Telefonu direksiyon gibi yatır. Hassasiyet eğim miktarını ayarlar.", "Tilt the phone like a wheel. Sensitivity sets how far you tilt.");
                default: return Loc.T("Ekranın sol yarısına dokun, sağa-sola kaydır. Uzaklık = direksiyon açısı.", "Touch the left half and slide left or right. Distance = steering angle.");
            }
        }
    }
}
