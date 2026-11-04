using System;
using System.Collections.Generic;
using UnityEngine;

namespace DerbyArena.Core
{
    /// <summary>Cosmetic slots of the player's car. One item of each slot is equipped at a time.</summary>
    public enum PartSlot { Paint = 0, Rim = 1, Neon = 2, Stripe = 3, Roof = 4, Spoiler = 5, Bumper = 6, Exhaust = 7 }

    /// <summary>One purchasable cosmetic. Price 0 = owned from the start (the stock look of each slot).</summary>
    public sealed class GarageItem
    {
        public string Id;
        public PartSlot Slot;
        public string NameTr, NameEn;
        public int Price;
        public int StarsNeeded;
        public Color Color = Color.white;
        public int Style;              // slot specific variant (see VehicleKit.KitStyle)

        public string Name { get { return Loc.T(NameTr, NameEn); } }
    }

    /// <summary>
    /// 4.0 garage shop: paint, rims, neon underglow, stripes, roof gear, spoilers, bumpers and exhausts bought
    /// with scrap; some pieces also ask for a star total so the shop grows with the campaign. The catalogue is
    /// data; VehicleKit / VehicleVisual turn the equipped items into geometry, materials and lights.
    /// </summary>
    public static class Garage
    {
        public const int SlotCount = 8;

        static GarageItem P(string id, string tr, string en, int price, int stars, Color c, int style = 0, PartSlot slot = PartSlot.Paint)
        {
            return new GarageItem { Id = id, Slot = slot, NameTr = tr, NameEn = en, Price = price, StarsNeeded = stars, Color = c, Style = style };
        }

        static readonly Color NoColor = new Color(0f, 0f, 0f, 0f);

        public static readonly GarageItem[] Catalog =
        {
            // paint (the stock orange is the Interceptor's own colour)
            P("paint_ember",   "KOR TURUNCU",     "EMBER ORANGE",     0,    0,  new Color(0.80f, 0.38f, 0.12f)),
            P("paint_crimson", "KIZIL",           "CRIMSON",          600,  0,  new Color(0.72f, 0.12f, 0.14f)),
            P("paint_cobalt",  "KOBALT MAVİ",     "COBALT BLUE",      600,  0,  new Color(0.15f, 0.35f, 0.85f)),
            P("paint_ice",     "BUZ BEYAZI",      "ICE WHITE",        900,  6,  new Color(0.90f, 0.92f, 0.95f)),
            P("paint_lime",    "ASİT YEŞİLİ",     "ACID LIME",        900,  12, new Color(0.55f, 0.85f, 0.15f)),
            P("paint_night",   "GECE SİYAHI",     "NIGHT BLACK",      1200, 20, new Color(0.08f, 0.08f, 0.10f)),
            P("paint_army",    "ORDU YEŞİLİ",     "ARMY GREEN",       1500, 30, new Color(0.35f, 0.42f, 0.25f)),
            P("paint_cyan",    "NEON CAMGÖBEĞİ",  "NEON CYAN",        1800, 36, new Color(0.10f, 0.80f, 0.85f)),
            P("paint_rose",    "PEMBE KROM",      "ROSE CHROME",      1800, 36, new Color(0.95f, 0.35f, 0.60f)),
            P("paint_gold",    "ALTIN",           "GOLD",             2500, 48, new Color(0.95f, 0.75f, 0.25f)),
            P("paint_violet",  "ELİT MORU",       "ELITE VIOLET",     2500, 60, new Color(0.45f, 0.15f, 0.70f)),
            P("paint_chrome",  "KROM",            "CHROME",           4000, 90, new Color(0.78f, 0.80f, 0.84f)),

            // rims (wheel hub / spoke colour)
            P("rim_steel",  "ÇELİK JANT",   "STEEL RIMS",   0,    0,  new Color(0.75f, 0.75f, 0.78f), 0, PartSlot.Rim),
            P("rim_black",  "MAT SİYAH",    "MATTE BLACK",  500,  0,  new Color(0.10f, 0.10f, 0.11f), 0, PartSlot.Rim),
            P("rim_red",    "KIRMIZI",      "RED",          800,  6,  new Color(0.85f, 0.12f, 0.12f), 0, PartSlot.Rim),
            P("rim_cyan",   "NEON",         "NEON",         1200, 15, new Color(0.15f, 0.85f, 0.90f), 0, PartSlot.Rim),
            P("rim_gold",   "ALTIN",        "GOLD",         1500, 25, new Color(0.95f, 0.72f, 0.20f), 0, PartSlot.Rim),
            P("rim_chrome", "KROM",         "CHROME",       2000, 40, new Color(0.95f, 0.96f, 1.00f), 0, PartSlot.Rim),

            // neon underglow (colour; style 1 = pulsing)
            P("neon_none",    "NEON YOK",        "NO NEON",         0,    0,  NoColor, 0, PartSlot.Neon),
            P("neon_cyan",    "CAMGÖBEĞİ NEON",  "CYAN NEON",       800,  10, new Color(0.20f, 0.90f, 1.00f), 0, PartSlot.Neon),
            P("neon_magenta", "MAGENTA NEON",    "MAGENTA NEON",    800,  10, new Color(1.00f, 0.25f, 0.85f), 0, PartSlot.Neon),
            P("neon_lime",    "ASİT NEON",       "ACID NEON",       800,  10, new Color(0.55f, 1.00f, 0.20f), 0, PartSlot.Neon),
            P("neon_amber",   "KEHRİBAR NEON",   "AMBER NEON",      800,  10, new Color(1.00f, 0.65f, 0.15f), 0, PartSlot.Neon),
            P("neon_red",     "KIZIL NEON",      "RED NEON",        1000, 20, new Color(1.00f, 0.15f, 0.15f), 0, PartSlot.Neon),
            P("neon_white",   "BEYAZ NEON",      "WHITE NEON",      1200, 30, new Color(0.95f, 0.97f, 1.00f), 0, PartSlot.Neon),
            P("neon_pulse",   "NABIZ NEON",      "PULSE NEON",      2000, 50, new Color(0.60f, 0.30f, 1.00f), 1, PartSlot.Neon),

            // stripes: style 1 double centre, 2 single wide, 3 offset racing stripe
            P("stripe_classic", "KLASİK ÇİFT ŞERİT", "CLASSIC TWIN STRIPE", 0,   0,  new Color(0.95f, 0.93f, 0.88f), 1, PartSlot.Stripe),
            P("stripe_none",    "ŞERİT YOK",         "NO STRIPE",           0,   0,  NoColor, 0, PartSlot.Stripe),
            P("stripe_black",   "GENİŞ SİYAH",       "WIDE BLACK",          400, 0,  new Color(0.06f, 0.06f, 0.07f), 2, PartSlot.Stripe),
            P("stripe_red",     "YARIŞ KIRMIZISI",   "RACING RED",          400, 4,  new Color(0.85f, 0.10f, 0.12f), 3, PartSlot.Stripe),
            P("stripe_cyan",    "ÇİFT CAMGÖBEĞİ",    "TWIN CYAN",           600, 10, new Color(0.15f, 0.85f, 0.90f), 1, PartSlot.Stripe),
            P("stripe_white",   "YARIŞ BEYAZI",      "RACING WHITE",        600, 10, new Color(0.95f, 0.95f, 0.95f), 3, PartSlot.Stripe),
            P("stripe_gold",    "GENİŞ ALTIN",       "WIDE GOLD",           900, 25, new Color(0.95f, 0.75f, 0.25f), 2, PartSlot.Stripe),

            // roof: 1 light bar, 0 none, 2 roof rack, 3 siren bar
            P("roof_lightbar", "FAR ÇUBUĞU",  "LIGHT BAR",  0,    0,  Color.white, 1, PartSlot.Roof),
            P("roof_none",     "TEMİZ TAVAN", "CLEAN ROOF", 0,    0,  Color.white, 0, PartSlot.Roof),
            P("roof_rack",     "ÇATI RAFI",   "ROOF RACK",  700,  8,  Color.white, 2, PartSlot.Roof),
            P("roof_siren",    "SİREN ÇUBUĞU","SIREN BAR",  1500, 35, Color.white, 3, PartSlot.Roof),

            // spoiler: 1 low wing, 0 none, 2 high wing, 3 ducktail
            P("spoiler_low",      "ALÇAK KANAT", "LOW WING",  0,   0,  Color.white, 1, PartSlot.Spoiler),
            P("spoiler_none",     "KANAT YOK",   "NO WING",   0,   0,  Color.white, 0, PartSlot.Spoiler),
            P("spoiler_ducktail", "ÖRDEK KUYRUĞU","DUCKTAIL", 600, 6,  Color.white, 3, PartSlot.Spoiler),
            P("spoiler_high",     "YÜKSEK KANAT","HIGH WING", 900, 15, Color.white, 2, PartSlot.Spoiler),

            // bumper: 1 bull bar, 0 none, 2 steel plough, 3 spiked bar
            P("bumper_bull",   "BULL BAR",     "BULL BAR",     0,    0,  Color.white, 1, PartSlot.Bumper),
            P("bumper_none",   "TAMPON YOK",   "STOCK NOSE",   0,    0,  Color.white, 0, PartSlot.Bumper),
            P("bumper_plough", "ÇELİK PULLUK", "STEEL PLOUGH", 1800, 30, Color.white, 2, PartSlot.Bumper),
            P("bumper_spikes", "DİKENLİ BAR",  "SPIKED BAR",   2500, 50, Color.white, 3, PartSlot.Bumper),

            // exhaust: 0 stock (hidden), 1 side pipes, 2 stacks, 3 flame pipes
            P("exhaust_stock",  "STANDART EGZOZ", "STOCK EXHAUST", 0,    0,  Color.white, 0, PartSlot.Exhaust),
            P("exhaust_side",   "YAN EGZOZ",      "SIDE PIPES",    800,  8,  Color.white, 1, PartSlot.Exhaust),
            P("exhaust_stacks", "BACA EGZOZ",     "STACKS",        1500, 25, Color.white, 2, PartSlot.Exhaust),
            P("exhaust_flame",  "ALEVLİ EGZOZ",   "FLAME PIPES",   2200, 45, Color.white, 3, PartSlot.Exhaust),
        };

        static readonly Dictionary<string, GarageItem> byId = new Dictionary<string, GarageItem>();

        static GarageItem Lookup(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (byId.Count == 0) foreach (var it in Catalog) byId[it.Id] = it;
            GarageItem r;
            return byId.TryGetValue(id, out r) ? r : null;
        }

        public static GarageItem Find(string id) { return Lookup(id); }

        public static string SlotName(PartSlot slot)
        {
            switch (slot)
            {
                case PartSlot.Paint: return Loc.T("BOYA", "PAINT");
                case PartSlot.Rim: return Loc.T("JANT", "RIMS");
                case PartSlot.Neon: return Loc.T("NEON", "NEON");
                case PartSlot.Stripe: return Loc.T("ŞERİT", "STRIPES");
                case PartSlot.Roof: return Loc.T("TAVAN", "ROOF");
                case PartSlot.Spoiler: return Loc.T("KANAT", "WING");
                case PartSlot.Bumper: return Loc.T("TAMPON", "BUMPER");
                default: return Loc.T("EGZOZ", "EXHAUST");
            }
        }

        public static List<GarageItem> Items(PartSlot slot)
        {
            var list = new List<GarageItem>();
            foreach (var it in Catalog) if (it.Slot == slot) list.Add(it);
            return list;
        }

        /// <summary>The first item of a slot is its stock look.</summary>
        public static GarageItem Default(PartSlot slot)
        {
            foreach (var it in Catalog) if (it.Slot == slot) return it;
            return null;
        }

        public static bool Owned(string id)
        {
            var it = Lookup(id);
            if (it == null) return false;
            if (it.Price == 0) return true;
            var owned = Save.Data.Owned;
            if (owned == null) return false;
            for (int i = 0; i < owned.Length; i++) if (owned[i] == id) return true;
            return false;
        }

        public static bool StarsOk(GarageItem it) { return it != null && Save.TotalStars() >= it.StarsNeeded; }

        /// <summary>Garage page preview: an item shown on the showcase before it is bought (cleared on leaving).</summary>
        public static GarageItem Preview;

        public static GarageItem Equipped(PartSlot slot)
        {
            if (Preview != null && Preview.Slot == slot) return Preview;
            var eq = Save.Data.Equipped;
            int i = (int)slot;
            if (eq != null && i < eq.Length)
            {
                var it = Lookup(eq[i]);
                if (it != null && it.Slot == slot && Owned(it.Id)) return it;
            }
            return Default(slot);
        }

        /// <summary>The equipped item ignoring any preview.</summary>
        public static GarageItem EquippedSaved(PartSlot slot)
        {
            var keep = Preview; Preview = null;
            var r = Equipped(slot);
            Preview = keep;
            return r;
        }

        public static bool IsEquipped(string id)
        {
            var it = Lookup(id);
            return it != null && Equipped(it.Slot).Id == id;
        }

        /// <summary>Buys an item with scrap (false when unknown, owned already, star gated or unaffordable).</summary>
        public static bool TryBuy(string id)
        {
            var it = Lookup(id);
            if (it == null || Owned(id) || !StarsOk(it) || Save.Data.Scrap < it.Price) return false;
            Save.Data.Scrap -= it.Price;
            Save.Data.Owned = Append(Save.Data.Owned, id);
            Equip(id);
            Save.Write();
            return true;
        }

        public static bool Equip(string id)
        {
            var it = Lookup(id);
            if (it == null || !Owned(id)) return false;
            var eq = Save.Data.Equipped;
            if (eq == null || eq.Length != SlotCount)
            {
                var n = new string[SlotCount];
                if (eq != null) Array.Copy(eq, n, Mathf.Min(eq.Length, SlotCount));
                eq = Save.Data.Equipped = n;
            }
            eq[(int)it.Slot] = id;
            Save.Write();
            return true;
        }

        internal static string[] Append(string[] arr, string id)
        {
            var list = new List<string>(arr ?? new string[0]);
            if (!list.Contains(id)) list.Add(id);
            return list.ToArray();
        }
    }

    /// <summary>A player vehicle in the collection: price, star gate and the spec factory.</summary>
    public sealed class VehicleDef
    {
        public string Id;
        public string NameTr, NameEn;
        public string BlurbTr, BlurbEn;
        public int Price;
        public int StarsNeeded;
        public int BossesNeeded;                   // season bosses that must be beaten (T-12: all five)
        public Func<DerbyArena.Vehicles.VehicleSpec> Make;
        public int Speed, Armor, Firepower, Handling;   // 1-5 for the garage bars
        public bool Tank;                           // tracked vehicle: no car kit, fixed camouflage paint

        public string Name { get { return Loc.T(NameTr, NameEn); } }
        public string Blurb { get { return Loc.T(BlurbTr, BlurbEn); } }
    }

    /// <summary>
    /// The vehicle collection (4.0): the Interceptor is free, the four rival chassis are bought with scrap and star
    /// totals, the War Rig and the Tiger I "T-12" sit at the end of the campaign. Prices rise steeply on purpose:
    /// the last two need a real investment of play.
    /// </summary>
    public static class Fleet
    {
        public const string DefaultId = "interceptor";

        public static readonly VehicleDef[] Defs =
        {
            new VehicleDef { Id = "interceptor", NameTr = "BREAKLINE INTERCEPTOR", NameEn = "BREAKLINE INTERCEPTOR",
                BlurbTr = "Dengeli muscle car: hız, zırh ve ateş gücü ortada. Her şeye uyar.",
                BlurbEn = "Balanced muscle car: speed, armour and firepower in the middle. Fits everything.",
                Price = 0, StarsNeeded = 0, Make = DerbyArena.Vehicles.VehicleSpec.Interceptor, Speed = 3, Armor = 3, Firepower = 3, Handling = 3 },
            new VehicleDef { Id = "drifter", NameTr = "RAZOR DRIFTER", NameEn = "RAZOR DRIFTER",
                BlurbTr = "Hafif ve çevik: en hızlı dönüş, en uzun drift. Zırhı ince, vurulmadan vur.",
                BlurbEn = "Light and nimble: sharpest turns, longest drifts. Thin armour, so hit before you get hit.",
                Price = 3500, StarsNeeded = 24, Make = DerbyArena.Vehicles.VehicleSpec.PlayerDrifter, Speed = 4, Armor = 2, Firepower = 2, Handling = 5 },
            new VehicleDef { Id = "rammer", NameTr = "BASTION RAMMER", NameEn = "BASTION RAMMER",
                BlurbTr = "Ağır gövde, güçlendirilmiş burun: çarpma hasarı %40 fazla, can yüksek, dönüş yavaş.",
                BlurbEn = "Heavy body, reinforced nose: ramming hurts 40% more, lots of health, slow turning.",
                Price = 8000, StarsNeeded = 54, Make = DerbyArena.Vehicles.VehicleSpec.PlayerRammer, Speed = 2, Armor = 5, Firepower = 3, Handling = 2 },
            new VehicleDef { Id = "hunter", NameTr = "SIEGE HUNTER", NameEn = "SIEGE HUNTER",
                BlurbTr = "Uzun menzilli top ve +2 roket: uzaktan sök, sonra yaklaş.",
                BlurbEn = "Long-range gun and +2 rockets: strip them from afar, then close in.",
                Price = 15000, StarsNeeded = 96, Make = DerbyArena.Vehicles.VehicleSpec.PlayerHunter, Speed = 3, Armor = 3, Firepower = 5, Handling = 3 },
            new VehicleDef { Id = "warrig", NameTr = "WAR RIG", NameEn = "WAR RIG",
                BlurbTr = "Patronların kamyonu: 420 can, çarpınca ezer, 6 roket. Yavaş ama durdurulamaz.",
                BlurbEn = "The bosses' truck: 420 health, crushes on contact, 6 rockets. Slow but unstoppable.",
                Price = 35000, StarsNeeded = 160, BossesNeeded = 3, Make = DerbyArena.Vehicles.VehicleSpec.PlayerWarRig, Speed = 2, Armor = 5, Firepower = 4, Handling = 1 },
            new VehicleDef { Id = "t12", NameTr = "TIGER I  ·  T-12", NameEn = "TIGER I  ·  T-12",
                BlurbTr = "Oyunun son aracı: Panzerkampfwagen VI Tiger I. 8,8 cm KwK 36 top, 650 can, paletler her şeyi ezer. Antende Türk bayrağı.",
                BlurbEn = "The endgame vehicle: Panzerkampfwagen VI Tiger I. 8.8 cm KwK 36 gun, 650 health, tracks crush everything. Turkish flag on the antenna.",
                Price = 75000, StarsNeeded = 230, BossesNeeded = 5, Make = DerbyArena.Vehicles.VehicleSpec.TigerT12, Speed = 1, Armor = 5, Firepower = 5, Handling = 1, Tank = true },
        };

        public static VehicleDef Find(string id)
        {
            for (int i = 0; i < Defs.Length; i++) if (Defs[i].Id == id) return Defs[i];
            return null;
        }

        /// <summary>Garage page preview of an unowned vehicle (cleared on leaving the page and before a match).</summary>
        public static string PreviewId;

        public static VehicleDef Current()
        {
            if (!string.IsNullOrEmpty(PreviewId)) { var p = Find(PreviewId); if (p != null) return p; }
            var d = Find(Save.Data.Vehicle);
            return d != null && Owned(d.Id) ? d : Defs[0];
        }

        public static void ClearPreview() { PreviewId = null; Garage.Preview = null; }

        public static bool Owned(string id)
        {
            if (id == DefaultId) return true;
            var owned = Save.Data.Owned;
            if (owned == null) return false;
            for (int i = 0; i < owned.Length; i++) if (owned[i] == "vehicle:" + id) return true;
            return false;
        }

        /// <summary>Season bosses beaten so far (the boss is the last level of each released season).</summary>
        public static int BossesBeaten()
        {
            int n = 0;
            for (int c = 1; c <= Campaign.Seasons; c++) if (Save.Data.Stars[c * Campaign.PerChapter] > 0) n++;
            return n;
        }

        public static bool GatesOk(VehicleDef d)
        {
            return d != null && Save.TotalStars() >= d.StarsNeeded && BossesBeaten() >= d.BossesNeeded;
        }

        public static bool TryBuy(string id)
        {
            var d = Find(id);
            if (d == null || Owned(id) || !GatesOk(d) || Save.Data.Scrap < d.Price) return false;
            Save.Data.Scrap -= d.Price;
            Save.Data.Owned = Garage.Append(Save.Data.Owned, "vehicle:" + id);
            Save.Data.Vehicle = id;
            Save.Write();
            return true;
        }

        public static bool Select(string id)
        {
            if (!Owned(id)) return false;
            Save.Data.Vehicle = id;
            Save.Write();
            return true;
        }
    }
}
