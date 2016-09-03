﻿using Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Maps
{
    internal delegate bool WorldFilter(World w);

    public class World
    {
        internal Sector Sector { get; set; }

        public World()
        {
            // Defaults for auto-properties
            Name = "";
            UWP = "X000000-0";
            PBG = "000";
            Bases = "";
            Zone = "";
            Allegiance = "Na";
            Stellar = "";
        }

        public string Name { get; set; }

        public string Hex
        {
            get { return hex.ToString(); }
            set { hex = new Hex(value); }
        }

        internal string SubsectorHex { get { return hex.ToSubsectorString(); } }

        [XmlElement("UWP"),JsonName("UWP")]
        public string UWP { get; set; }

        [XmlElement("PBG"), JsonName("PBG")]
        public string PBG { get; set; }
        public string Zone
        {
            get { return zone; }
            set
            {
                zone = (value == " " || value == "G") ? string.Empty : value;
            }
        }
        private string zone;

        public string Bases { get; set; }
        public string Allegiance { get; set; }
        public string Stellar { get; set; }

        // T5
        public string SS
        {
            get
            {
                return "" + (char)('A' + Subsector);
            }
        }

        [XmlElement("Ix"), JsonName("Ix")]
        public string Importance { get; set; }

        internal int? ImportanceValue
        {
            get
            {
                int? value = null;
                int tmp;
                string ix = Importance;
                if (!string.IsNullOrWhiteSpace(ix) && int.TryParse(ix.Replace('{', ' ').Replace('}', ' '),
                    NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out tmp))
                {
                    value = tmp;
                }
                return value;
            }
        }

        [XmlElement("Ex"), JsonName("Ex")]
        public string Economic { get; set; }
        [XmlElement("Cx"), JsonName("Cx")]
        public string Cultural { get; set; }
        public string Nobility { get; set; }
        public byte Worlds { get; set; }
        public int ResourceUnits { get; set; }

        private Hex hex;
        internal byte X { get { return hex.X; } }
        internal byte Y { get { return hex.Y; } }

        public int Subsector
        {
            get
            {
                return ((X - 1) / Astrometrics.SubsectorWidth) + ((Y - 1) / Astrometrics.SubsectorHeight) * 4;
            }
        }

        public int Quadrant
        {
            get
            {
                return ((X - 1) / (Astrometrics.SubsectorWidth * 2)) + ((Y - 1) / (Astrometrics.SubsectorHeight * 2)) * 4;
            }
        }


        internal Point Coordinates
        {
            get
            {
                if (Sector == null)
                    throw new InvalidOperationException("Can't get coordinates for a world not assigned to a sector");

                return Astrometrics.LocationToCoordinates(Sector.Location, new Hex(X, Y));
            }
        }

        internal char Starport { get { return UWP[0]; } }
        internal int Size { get { return Char.ToUpperInvariant(UWP[1]) == 'S' ? -1 : SecondSurvey.FromHex(UWP[1], valueIfUnknown: -1); } }
        internal int Atmosphere { get { return SecondSurvey.FromHex(UWP[2], valueIfUnknown: -1); } }
        internal int Hydrographics { get { return SecondSurvey.FromHex(UWP[3], valueIfUnknown: -1); } }
        internal int PopulationExponent { get { return SecondSurvey.FromHex(UWP[4], valueIfUnknown: 0); } }
        internal int Government { get { return SecondSurvey.FromHex(UWP[5], valueIfUnknown: 0); } }
        internal int Law { get { return SecondSurvey.FromHex(UWP[6], valueIfUnknown: 0); } }
        internal int TechLevel { get { return SecondSurvey.FromHex(UWP[8], valueIfUnknown: 0); } }

        internal int PopulationMantissa
        {
            get
            {
                int mantissa = SecondSurvey.FromHex(PBG[0], valueIfUnknown: 0);
                // Hack for legacy data w/o PBG
                if (mantissa == 0 && PopulationExponent > 0)
                    return 1;
                return mantissa;
            }
        }

        internal int Belts { get { return SecondSurvey.FromHex(PBG[1], valueIfUnknown: 0); } }
        internal int GasGiants { get { return SecondSurvey.FromHex(PBG[2], valueIfUnknown: 0); } }
        internal double Population { get { return Math.Pow(10, PopulationExponent) * PopulationMantissa; } }
        internal bool WaterPresent { get { return (Hydrographics > 0) && (Util.InRange(Atmosphere, 2, 9) || Util.InRange(Atmosphere, 0xD, 0xF)); } }
        internal bool IsBa { get { return PopulationExponent == 0; } }
        internal bool IsLo { get { return PopulationExponent < 4; } }
        internal bool IsHi { get { return PopulationExponent >= 9; } }

        internal bool IsAg { get { return Util.InRange(Atmosphere, 4, 9) && Util.InRange(Hydrographics, 4, 8) && Util.InRange(PopulationExponent, 5, 7); } }
        internal bool IsNa { get { return Util.InRange(Atmosphere, 0, 3) && Util.InRange(Hydrographics, 0, 3) && Util.InRange(PopulationExponent, 6, 10); } }
        internal bool IsIn { get { return Util.InList(Atmosphere, 0, 1, 2, 4, 7, 9) && Util.InList(PopulationExponent, 9, 10); } }
        internal bool IsNi { get { return Util.InRange(PopulationExponent, 1, 6); } }
        internal bool IsRi { get { return Util.InList(Atmosphere, 6, 7, 8) && Util.InList(PopulationExponent, 6, 7, 8) && Util.InList(Government, 4, 5, 6, 7, 8, 9); } }
        internal bool IsPo { get { return Util.InList(Atmosphere, 2, 3, 4, 5) && Util.InList(Hydrographics, 0, 1, 2, 3) && PopulationExponent > 0; } }

        internal bool IsWa { get { return Hydrographics == 10; } }
        internal bool IsDe { get { return Util.InRange(Atmosphere, 2, 10) && Hydrographics == 0; } }
        internal bool IsAs { get { return Size == 0; } }
        internal bool IsVa { get { return Util.InRange(Size, 1, 10) && Atmosphere == 0; } }
        internal bool IsIc { get { return Util.InList(Atmosphere, 0, 1) && Util.InRange(Hydrographics, 1, 10); } }
        internal bool IsFl { get { return Atmosphere == 10 && Util.InRange(Hydrographics, 1, 10); } }

        internal bool IsCp { get { return HasCode("Cp"); } }
        internal bool IsCs { get { return HasCode("Cs"); } }
        internal bool IsCx { get { return HasCode("Cx"); } }

        internal bool IsPenalColony { get { return HasCode("Pe"); } }
        internal bool IsReserve { get { return HasCode("Re"); } }
        internal bool IsPrisonExileCamp { get { return HasCode("Px") || HasCode("Ex"); } } // Px is T5, Ex is legacy
        // TODO: "Pr" is used in some legacy files, conflicts with T5 "Pre-Rich" - convert codes on import/export
        internal string ResearchStation { get { return GetCodePrefix("Rs"); } }

        internal bool IsPlaceholder { get { return UWP == "XXXXXXX-X" || UWP == "???????-?"; } }

        internal bool IsCapital
        {
            get
            {
                return codes.Any(s => s == "Cp" || s == "Cs" || s == "Cx" || s == "Capital");
            }
        }

        public bool HasCode(string code)
        {
            if (code == null)
                throw new ArgumentNullException("code");

            return codes.Any(s => s.Equals(code, StringComparison.InvariantCultureIgnoreCase));
        }

        public string GetCodePrefix(string code)
        {
            if (code == null)
                throw new ArgumentNullException("code");

            return codes.Where(s => s.StartsWith(code, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        public bool HasCodePrefix(string code)
        {
            if (code == null)
                throw new ArgumentNullException("code");

            return codes.Any(s => s.StartsWith(code, StringComparison.InvariantCultureIgnoreCase));
        }

        // Keep storage as a string and parse on demand to reduce memory consumption.
        // (Many worlds * many codes = lots of strings and container overhead)
        // "[Sophont]" - major race homeworld
        // "(Sophont)" - minor race homeworld
        // "(Sophont)0" - minor race homeworld (population in tenths)
        // "{comment ... }" - arbitrary comment
        // "xyz" - other code
        private class CodeList : IEnumerable<string>
        {
            public CodeList(string codes = "") { this.codes = codes; }
            private string codes;

            public IEnumerator<string> GetEnumerator()
            {
                int pos = 0;                
                while (pos < codes.Length)
                {
                    int begin = pos;
                    switch (codes[pos++])
                    {
                        case ' ':
                            continue;
                        case '[':
                            while (pos < codes.Length && codes[pos] != ']') ++pos;
                            while (pos < codes.Length && codes[pos] != ' ') ++pos;
                            break;
                        case '(':
                            while (pos < codes.Length && codes[pos] != ')') ++pos;
                            while (pos < codes.Length && codes[pos] != ' ') ++pos;
                            break;
                        case '{':
                            while (pos < codes.Length && codes[pos] != '}') ++pos;
                            while (pos < codes.Length && codes[pos] != ' ') ++pos;
                            break;
                        default:
                            while (pos < codes.Length && codes[pos] != ' ') ++pos;
                            break;
                    }
                    yield return codes.Substring(begin, pos - begin);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }

        public string Remarks
        {
            get { return string.Join(" ", codes); }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                codes = new CodeList(value);
            }
        }

        internal IEnumerable<string> Codes { get { return codes; } }
        private CodeList codes = new CodeList();

        public string LegacyBaseCode
        {
            get { return SecondSurvey.EncodeLegacyBases(Allegiance, Bases); }
            set { Bases = SecondSurvey.DecodeLegacyBases(Allegiance, value); }
        }

        internal bool IsAmber { get { return Zone == "A" || Zone == "U"; } }
        internal bool IsRed { get { return Zone == "R" || Zone == "F"; } }
        internal bool IsBlue { get { return Zone == "B"; } } // TNE Technologically Elevated Dictatorship

        [XmlAttribute("Sector"), JsonName("Sector")]
        public string SectorName { get { return Sector.Names[0].Text; } }

        public string SubsectorName
        {
            get
            {
                return Sector.Subsector(Subsector)?.Name ?? "";
            }
        }

        public string AllegianceName
        {
            get
            {
                return Sector?.GetAllegianceFromCode(Allegiance)?.Name ?? "";
            }
        }

        internal string BaseAllegiance
        {
            get
            {
                if (Sector == null)
                    return Allegiance;
                return Sector.AllegianceCodeToBaseAllegianceCode(Allegiance);
            }
        }

        internal string LegacyAllegiance
        {
            get
            {
                return SecondSurvey.T5AllegianceCodeToLegacyCode(Allegiance);
            }
        }

    }
}
