using System;
using System.Collections.Generic;
using System.Text;

namespace PointApp.Models
{
    public class PlayerInfo
    {
        private double ConvertStrPointToDouble(string strPoint)
        {
            string strValue = strPoint;
            int pos = strPoint.Length - 2;
            if (strPoint.Length == 4 && pos > 0 && strPoint[pos] != '.')
            {
                strValue = strPoint.Insert(pos, ".");
            }
            return double.Parse(strValue);
        }

        public double FisDh
        {
            get
            {
                double dValue = 330.00;
                if (!string.IsNullOrWhiteSpace(StrFisDh))
                {
                    dValue = ConvertStrPointToDouble(StrFisDh);
                }
                return dValue;
            }
        }

        public double FisGs
        {
            get
            {
                double dValue = 220.00;
                if (!string.IsNullOrWhiteSpace(StrFisGs))
                {
                    dValue = ConvertStrPointToDouble(StrFisGs);
                }
                return dValue;
            }
        }

        public double FisSc
        {
            get
            {
                double dValue = 270.00;
                if (!string.IsNullOrWhiteSpace(StrFisSc))
                {
                    dValue = ConvertStrPointToDouble(StrFisSc);
                }
                return dValue;
            }
        }

        public double FisSg
        {
            get
            {
                double dValue = 270.00;
                if (!string.IsNullOrWhiteSpace(StrFisSg))
                {
                    dValue = ConvertStrPointToDouble(StrFisSg);
                }
                return dValue;
            }
        }

        public double FisSl
        {
            get
            {
                double dValue = 165.00;
                if (!string.IsNullOrWhiteSpace(StrFisSl))
                {
                    dValue = ConvertStrPointToDouble(StrFisSl);
                }
                return dValue;
            }
        }

        public string JapaneseName { get; set; } = string.Empty;

        public string KanaName { get; set; } = string.Empty;

        public double SajDh
        {
            get
            {
                double dValue = 330.00;
                if (!string.IsNullOrWhiteSpace(StrSajDh))
                {
                    dValue = ConvertStrPointToDouble(StrSajDh);
                }
                return dValue;
            }
        }

        public double SajGs
        {
            get
            {
                double dValue = 220.00;
                if (!string.IsNullOrWhiteSpace(StrSajGs))
                {
                    dValue = ConvertStrPointToDouble(StrSajGs);
                }
                return dValue;
            }
        }

        public double SajSc
        {
            get
            {
                double dValue = 270.00;
                if (!string.IsNullOrWhiteSpace(StrSajSc))
                {
                    dValue = ConvertStrPointToDouble(StrSajSc);
                }
                return dValue;
            }
        }

        public double SajSg
        {
            get
            {
                double dValue = 270.00;
                if (!string.IsNullOrWhiteSpace(StrSajSg))
                {
                    dValue = ConvertStrPointToDouble(StrSajSg);
                }
                return dValue;
            }
        }

        public double SajSl
        {
            get
            {
                double dValue = 165.00;
                if (!string.IsNullOrWhiteSpace(StrSajSl))
                {
                    dValue = ConvertStrPointToDouble(StrSajSl);
                }
                return dValue;
            }
        }

        public string StrFisDh { get; set; } = string.Empty;
        public string StrFisSl { get; set; } = string.Empty;

        public string StrFisGs { get; set; } = string.Empty;

        public string StrFisSc { get; set; } = string.Empty;

        public string StrFisSg { get; set; } = string.Empty;

        public string StrSajDh { get; set; } = string.Empty;

        public string StrSajGs { get; set; } = string.Empty;

        public string StrSajSc { get; set; } = string.Empty;

        public string StrSajSg { get; set; } = string.Empty;

        public string StrSajSl { get; set; } = string.Empty;
        private double? _ResultSajPoint = null;

        public double? ResultSajPoint
        {
            get
            {
                if (_ResultSajPoint == null)
                {
                    return null;
                }
                else
                {
                    return (double)Math.Round((decimal)_ResultSajPoint, 2);
                }
            }
            set { _ResultSajPoint = value; }
        }

        private double? _ResultFisPoint = null;

        public double? ResultFisPoint
        {
            get
            {
                if (_ResultFisPoint == null)
                {
                    return null;
                }
                else
                {
                    return (double)Math.Round((decimal)_ResultFisPoint, 2);
                }
            }
            set { _ResultFisPoint = value; }
        }

        public double? Time { get; set; } = 120.00;

        public double? Diff { get; set; } = null;


        public PlayerInfo DeepCopy()
        {
            var player = MemberwiseClone() as PlayerInfo;
            return player;
        }
    }
}
