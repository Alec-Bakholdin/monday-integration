using System;
using System.Collections.Generic;
using System.Reflection;

namespace monday_integration.src.monday.model
{
    public class MondayParameterOptions
    {
        public string GetParameters() {
            var paramList = new List<string>();
            foreach(var propInfo in this.GetType().GetProperties()) {
                var value = propInfo.GetValue(this);
                var defaultValue = GetDefaultValue(propInfo.PropertyType);
                if(value != defaultValue) {
                    var keyValuePair = $"{propInfo.Name}: {StringifyValue(propInfo)}";
                    paramList.Add(keyValuePair);
                }
            }
            return string.Join(", ", paramList);
        }

        private string StringifyValue(PropertyInfo propInfo) {
            var propType = propInfo.PropertyType;
            var value = propInfo.GetValue(this);
            if(propType == typeof(string)) {
                return $"\"{value}\"";
            }
            return value.ToString();
        }

        private object GetDefaultValue(Type t) {
            if(t.IsValueType) {
                return Activator.CreateInstance(t);
            }
            return null;
        }
    }
}