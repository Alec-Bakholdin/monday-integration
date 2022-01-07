using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using RestSharp;
using RestSharp.Deserializers;

namespace monday_integration.src.api
{
    public class CsvDeserializer : IDeserializer
    {
        public T? Deserialize<T>(IRestResponse response)
        {
            var type = typeof(T);
            var genericTypes = type.GetGenericArguments();
            if(genericTypes.Length != 1 || type.GetGenericTypeDefinition() != typeof(List<>)) {
                throw new InvalidDataException("CSV response must be of type List<T>");
            }
            var internalType = genericTypes[0];

            var formattedContent = FormatContentHeader(response.Content);
            var reader = new StringReader(formattedContent);
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture);
            var csvReader = new CsvReader(reader, configuration);
            csvReader.Configuration.TypeConverterCache.AddConverter<DateTime?>(new CustomDateTimeConverter());
            csvReader.Configuration.TypeConverterCache.AddConverter<string>(new CustomStringDeserializer());

            var listOfObjects = csvReader.GetRecords(internalType);
            var listOfGenerics = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(internalType));
            foreach(var obj in listOfObjects) {
                listOfGenerics.Add(obj);
            }
            return (T)listOfGenerics;
        }

        private string FormatContentHeader(string content) {
            const string newLineDelim = "\n";
            const string entryDelim = ",";

            var contentArr = content.Split(newLineDelim);
            var contentHeader = contentArr[0];
            var contentData = contentArr[1..];
            
            var headers = contentHeader.Split(entryDelim);
            for(int i = 0; i < headers.Length; i++) {
                headers[i] = headers[i].Replace(" ", "");
            }
            contentArr[0] = String.Join(entryDelim, headers);
            return String.Join(newLineDelim, contentArr);
        }
    }
}