using System.Globalization;
using System.Text;
using OSIsoft.Data;

namespace SdsClientLibraries
{
    public class WaveDataInteger
    {
        [SdsMember(IsKey = true)]
        public int OrderTarget { get; set; }

        public int SinInt { get; set; }

        public int CosInt { get; set; }

        public int TanInt { get; set; }

        public override string ToString()
        {
            CultureInfo cultureInfo = CultureInfo.InvariantCulture;

            StringBuilder builder = new ();
            builder.Append(cultureInfo, $"OrderTarget: {OrderTarget}");
            builder.Append(cultureInfo, $", SinInt: {SinInt}");
            builder.Append(cultureInfo, $", CosInt: {CosInt}");
            builder.Append(cultureInfo, $", TanInt: {TanInt}");
            return builder.ToString();
        }
    }
}
