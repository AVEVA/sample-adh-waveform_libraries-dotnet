using System.Globalization;
using System.Text;
using OSIsoft.Data;

namespace SdsClientLibraries
{
    public class WaveDataTarget
    {
        [SdsMember(IsKey = true)]
        public int OrderTarget { get; set; }

        public double TauTarget { get; set; }

        public double RadiansTarget { get; set; }

        public double SinTarget { get; set; }

        public double CosTarget { get; set; }

        public double TanTarget { get; set; }

        public double SinhTarget { get; set; }

        public double CoshTarget { get; set; }

        public double TanhTarget { get; set; }

        public override string ToString()
        {
            CultureInfo cultureInfo = CultureInfo.InvariantCulture;

            StringBuilder builder = new ();
            builder.Append(cultureInfo, $"OrderTarget: {OrderTarget}");
            builder.Append(cultureInfo, $", RadiansTarget: {RadiansTarget}");
            builder.Append(cultureInfo, $", TauTarget: {TauTarget}");
            builder.Append(cultureInfo, $", SinTarget: {SinTarget}");
            builder.Append(cultureInfo, $", CosTarget: {CosTarget}");
            builder.Append(cultureInfo, $", TanTarget: {TanTarget}");
            builder.Append(cultureInfo, $", SinhTarget: {SinhTarget}");
            builder.Append(cultureInfo, $", CoshTarget: {CoshTarget}");
            builder.Append(cultureInfo, $", TanhTarget: {TanhTarget}");
            return builder.ToString();
        }
    }
}
