using System.IO;

namespace LEGOMaterials
{
    public static class MaterialPathUtility
    {
        static string materialsPath = "Packages/com.unity.lego.materials/Materials";
        static string biDir = "BI";
        static string legacyDir = "Legacy";


        public static string GetPath(MouldingColour.Id id, bool legacy = false, bool useBI = false)
        {
            return Path.Combine(MaterialPathUtility.materialsPath, useBI ? biDir : "", legacy ? legacyDir : "", (int)id + ".mat");
        }
    }    
}
