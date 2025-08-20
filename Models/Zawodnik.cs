using System;
using System.Collections.Generic;
using System.IO;

namespace ArenaFights.Models
{
    /// <summary>Zawodnik.</summary>
    public class Zawodnik
    {
        public string Imie { get; set; } = "";
        public string Druzyna { get; set; }  // może być null lub ""


        // Statystyki
        public int Sila { get; set; }
        public int Walka { get; set; }
        public int Energia { get; set; }
        public int Wytrzymalosc { get; set; }
        public int Predkosc { get; set; }

        public decimal Tony { get; set; }
        public decimal Elo { get; set; }
        public int Miejsce { get; set; }

        // Portret
        public string PortretPath { get; set; }  // względna (assets/portraits/...) lub absolutna
        public string PortretPathAbsolute
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PortretPath)) return null;
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(baseDir, PortretPath.Replace('/', Path.DirectorySeparatorChar));
            }
        }

        // Flagi do ekranu walki / oznaczeń
        public bool PortretL { get; set; } = true;   // domyślnie lewa zaznaczona
        public bool PortretR { get; set; } = false;

        // Umiejętności / słabości
        public List<UmiejetnoscRef> Umiejetnosci { get; set; } = new List<UmiejetnoscRef>();
        public List<UmiejetnoscRef> Slabosci { get; set; } = new List<UmiejetnoscRef>();
    }
}
