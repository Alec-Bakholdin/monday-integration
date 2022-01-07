using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace monday_integration.src.api
{
    public class CustomStringDeserializer : StringConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            return base.ConvertFromString(text.Trim(), row, memberMapData);
        }
    }
}