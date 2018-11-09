using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public static class AuxFunctions
    {
        //Get the directory of the project
        public static string GetProjPath()
        {
            string current = Directory.GetCurrentDirectory();

            DirectoryInfo binPath = Directory.GetParent(current);

            string binStringPath = binPath.ToString();

            DirectoryInfo classPath = Directory.GetParent(binStringPath);

            string classStringPath = classPath.ToString();

            DirectoryInfo projPath = Directory.GetParent(classStringPath);

            return projPath.ToString();


        }
    }
}
