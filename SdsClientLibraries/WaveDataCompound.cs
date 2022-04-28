using System.Globalization;
using System.Text;
using OSIsoft.Data;

namespace SdsClientLibraries
{
    public class WaveDataCompound
    {
        [SdsMember(IsKey = true, Order = 0)]
        public int Order { get; set; }

        [SdsMember(IsKey = true, Order = 1)]
        public int Multiplier { get; set; }

        public double Tau { get; set; }

        public double Radians { get; set; }

        public double Sin { get; set; }

        public double Cos { get; set; }

        public double Tan { get; set; }

        public double Sinh { get; set; }

        public double Cosh { get; set; }

        public double Tanh { get; set; }

        public override string ToString()
        {
            CultureInfo cultureInfo = CultureInfo.InvariantCulture;

            StringBuilder builder = new ();
            builder.Append(cultureInfo, $"Order: {Order}");
            builder.Append(cultureInfo, $", Multiplier: {Multiplier}");
            builder.Append(cultureInfo, $", Radians: {Radians}");
            builder.Append(cultureInfo, $", Tau: {Tau}");
            builder.Append(cultureInfo, $", Sin: {Sin}");
            builder.Append(cultureInfo, $", Cos: {Cos}");
            builder.Append(cultureInfo, $", Tan: {Tan}");
            builder.Append(cultureInfo, $", Sinh: {Sinh}");
            builder.Append(cultureInfo, $", Cosh: {Cosh}");
            builder.Append(cultureInfo, $", Tanh: {Tanh}");
            return builder.ToString();
        }
    }
}
