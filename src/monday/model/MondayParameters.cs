using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace monday_integration.src.monday.model
{
    public class MondayParameters<T>
    {   
        private T target {get; set;}

        public MondayParameters(T target) {
            this.target = target;
        }

        public string GetParameters() {
            var paramList = new List<string>();
            foreach(var propInfo in this.GetType().GetProperties()) {
                var genericArgs = propInfo.PropertyType.GetGenericArguments();
                if(genericArgs.Length != 2) {
                    throw new InvalidOperationException("MondayParameters property must be a Func<T, object> structure");
                }
                
                var resultStr = GetStringValue(propInfo.GetValue(this), genericArgs[1]);
                if(resultStr != null ){
                    var parameterPair = $"{propInfo.Name}: {resultStr}";
                    paramList.Add(parameterPair);
                }

            }
            return string.Join(", ", paramList);
        }

        public string GetStringValue(object function, Type returnType) {
            if(function == null) return null;

            if(returnType == typeof(string)) {
                return "\"" + ((Func<T, string>)function)(target) + "\"";
            }
            if(returnType == typeof(int?)) {
                return ((Func<T, int?>)function)(target)?.ToString();
            }
            if(returnType == typeof(int)) {
                return ((Func<T, int>)function)(target).ToString();
            }
            if(returnType == typeof(bool)) {
                return ((Func<T, bool>)function)(target) ? "true" : "false";
            }
            if(returnType == typeof(Dictionary<string, string>)) {
                var dict = ((Func<T, Dictionary<string, string>>)function)(target);
                var joinedDict = ((Dictionary<string, string>)dict).Select(pair => $"\"{pair.Key}\": {pair.Value}".Replace("\"", "\\\""));
                var joinedDictStr = String.Join(", ", joinedDict);
                return "\"{" + joinedDictStr + "}\"";
            }
            throw new InvalidOperationException($"Unsupported type {returnType.Name}");
        }
    }
}