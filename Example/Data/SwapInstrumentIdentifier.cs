using System;
using System.Collections.Generic;

namespace Puma.MDE.Data
{
    public class SwapInstrumentIdentifier
    {
        public string Ric { private set; get; }
        public int? Sicovam { private set; get; }
        public TypeManstInstrument InstrumentType { private set; get; } // intentionally not included in
                                                                        // in hashcode and equals

        public SwapInstrumentIdentifier(string ric, int? sicovam, TypeManstInstrument instrumentType)
        {
            Ric = ric;
            Sicovam = sicovam;
            InstrumentType = instrumentType;
        }

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                SwapInstrumentIdentifier otherIdent = (SwapInstrumentIdentifier)obj;
                return Object.Equals(Ric, otherIdent.Ric) && Object.Equals(Sicovam, otherIdent.Sicovam);
            }
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + Ric.GetHashCode();
            if (Sicovam.HasValue) hash = (hash * 7) + Sicovam.GetHashCode();
            return hash;
        }

        public override string ToString()
        {
            return string.Format("[Ric:{0}, Sicovam:{1}, Type:{2}]", Ric, Sicovam, InstrumentType);
        }

        public static string GetRicWithSicovam(List<SwapInstrumentIdentifier> identifiers, int sicovam)
        {
            foreach (var ident in identifiers)
            {
                if (ident.Sicovam.HasValue && ident.Sicovam.Value == sicovam) return ident.Ric;
            }
            return string.Empty;
        }

        public static SwapInstrumentIdentifier GetIdentifierWithSicovam(List<SwapInstrumentIdentifier> identifiers, int sicovam)
        {
            foreach (var ident in identifiers)
            {
                if (ident.Sicovam.HasValue && ident.Sicovam.Value == sicovam) return ident;
            }
            return null;
        }

        public static List<SwapInstrumentIdentifier> GetIdentifiersWithSicovam(List<SwapInstrumentIdentifier> identifiers, int sicovam)
        {
            List<SwapInstrumentIdentifier> result = new List<SwapInstrumentIdentifier>();
            foreach (var ident in identifiers)
            {
                if (ident.Sicovam.HasValue && ident.Sicovam.Value == sicovam) result.Add(ident);
            }
            return result;
        }


        public static SwapInstrumentIdentifier GetIdentifierWithRic(List<SwapInstrumentIdentifier> identifiers, string ric)
        {
            foreach (var ident in identifiers)
            {
                if (ident.Ric == ric) return ident;
            }
            return null;
        }

        public static List<SwapInstrumentIdentifier> GetIdentifiersWithRic(List<SwapInstrumentIdentifier> identifiers, string ric)
        {
            List<SwapInstrumentIdentifier> result = new List<SwapInstrumentIdentifier>();
            foreach (var ident in identifiers)
            {
                if (ident.Ric == ric) result.Add(ident);
            }
            return result;
        }


    }
}
