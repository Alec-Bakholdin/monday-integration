using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System;

/**
 * A collection of Functions that I think are 
 * useful when handling Newtonsoft JObjects and JArrays
 */
namespace Custom.Utility
{
    public static class JsonUtility
    {

        /**
         * Adds the objects in the JArray to a member of a dictionary
         * with the key being the value of the target field
         */
        public static Dictionary<string, JArray> AssociateJArray(JArray array, string targetField)
        {
            var targetFieldArr = new string[]{targetField};
            return AssociateJArray(array, targetFieldArr);
        }


        /**
         * Adds the objects in the JArray to a member of a dictionary
         * with the key being all the fields' values joined with
         * '\n'
         */
        public static Dictionary<string, JArray> AssociateJArray(JArray array, string[] targetFields)
        {
            var associatedDict = new Dictionary<string, JArray>();

            foreach(JObject obj in array)
            {
                // form new key, which is just the value in obj for each field in targetFields, joined with new lines
                var newKey = String.Join("\n", targetFields.Select(field => (string)obj[field]).ToList());

                // if the key doesn't exist, create a new JArray
                if(!associatedDict.ContainsKey(newKey))
                    associatedDict.Add(newKey, new JArray());

                // add object to its correct location
                associatedDict[newKey].Add(obj);
            }

            return associatedDict;
        }
    }
}