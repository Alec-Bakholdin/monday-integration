using System;
using System.Collections.Generic;

namespace monday_integration.src.monday.model
{
    public class MondayBodyOptions
    {

        public string GetBody() {
            var propValues = new List<string>();
            var properties = this.GetType().GetProperties();
            foreach(var prop in properties) {
                if(prop.PropertyType == typeof(bool) && (bool)prop.GetValue(this)) {
                    propValues.Add(prop.Name);
                } else if(IsBodyOptionsType(prop.PropertyType)) {
                    var bodyOptions = (MondayBodyOptions)prop.GetValue(this);
                    if(bodyOptions != null && bodyOptions.HasBody()) {
                        var bodyOptionsStr = $"{prop.Name}{{{bodyOptions.GetBody()}}}";
                        propValues.Add(bodyOptionsStr);
                    }
                }
            }
            return string.Join(", ", propValues);
        }

        private bool HasBody() {
            var body = this.GetBody();
            return body != null && body.Length > 0;
        }

        private bool IsBodyOptionsType(Type target){
            return target.IsSubclassOf(typeof(MondayBodyOptions)) || target == typeof(MondayBodyOptions);
        }
    }
}