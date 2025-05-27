using DataKeeper.Utility;
using UnityEngine;

namespace DataKeeper.Attributes
{
    public class CSVDataAttribute : PropertyAttribute
    {
        public readonly CSVDelimiterType DelimiterType;
        public CSVAssetReferenceType AssetReferenceType;

        public CSVDataAttribute(CSVDelimiterType csvDelimiterType = CSVDelimiterType.Tab, CSVAssetReferenceType assetReferenceType = CSVAssetReferenceType.AssetPath)
        {
            DelimiterType = csvDelimiterType;
            AssetReferenceType = assetReferenceType;
        }
    }
}
