using System;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace monday_integration.src.api
{
    public class CustomDateTimeConverter : DateTimeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if(text == "") {
                return null;
            }
            return base.ConvertFromString(text, row, memberMapData);
        }
    }
}