using DK.AppEnvironment;
using System;

namespace DK.Code
{
    public enum FileContext
    {
        ServerTrigger,
        ServerClass,
        ServerProgram,
        ClientTrigger,
        ClientClass,
        GatewayProgram,
        NeutralClass,
        Function,
        Dictionary,
        Include
    }

    public static class FileContextHelper
    {
        public static FileContext GetFileContextFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return FileContext.Include;

            var titleExt = PathUtil.GetFileName(fileName);
            if (titleExt.Equals("dict", StringComparison.OrdinalIgnoreCase) ||
                titleExt.Equals("dict+", StringComparison.OrdinalIgnoreCase) ||
                titleExt.Equals("dict&", StringComparison.OrdinalIgnoreCase))
            {
                return FileContext.Dictionary;
            }

            var ext = PathUtil.GetExtension(fileName);
            switch (ext.ToLower())
            {
                case ".sc":
                case ".sc+":
                case ".sc&":
                    return FileContext.ServerClass;
                case ".st":
                case ".st+":
                case ".st&":
                    return FileContext.ServerTrigger;
                case ".sp":
                case ".sp+":
                case ".sp&":
                    return FileContext.ServerProgram;
                case ".cc":
                case ".cc+":
                case ".cc&":
                    return FileContext.ClientClass;
                case ".ct":
                case ".ct+":
                case ".ct&":
                    return FileContext.ClientTrigger;
                case ".gp":
                case ".gp+":
                case ".gp&":
                    return FileContext.GatewayProgram;
                case ".nc":
                case ".nc+":
                case ".nc&":
                    return FileContext.NeutralClass;
                case ".f":
                case ".f+":
                case ".f&":
                    return FileContext.Function;
                default:
                    return FileContext.Include;
            }
        }

        public static bool IsClass(this FileContext fc)
        {
            return fc == FileContext.ServerClass || fc == FileContext.ClientClass || fc == FileContext.NeutralClass;
        }

        public static string GetClassNameFromFileName(string fileName)
        {
            var context = GetFileContextFromFileName(fileName);
            if (!context.IsClass()) return null;
            return PathUtil.GetFileNameWithoutExtension(fileName).ToLower();
        }

        public static bool IsLocalizedFile(string fileName)
        {
            var ext = PathUtil.GetExtension(fileName);
            return ext.EndsWith("+") || ext.EndsWith("&");
        }

        public static bool IncludeFileShouldBeMerged(string fileName) => PathUtil.GetExtension(fileName).EqualsI(".t");

        public static bool FileNameIsClass(string fileName, out string className)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                className = null;
                return false;
            }

            var ext = PathUtil.GetExtension(fileName).ToLower();
            switch (ext)
            {
                case ".cc":
                case ".cc&":
                case ".cc+":
                case ".nc":
                case ".nc&":
                case ".nc+":
                case ".sc":
                case ".sc&":
                case ".sc+":
                    className = PathUtil.GetFileNameWithoutExtension(fileName);
                    return true;
                default:
                    className = null;
                    return false;
            }
        }

        public static bool FileNameIsFunction(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;

            var ext = PathUtil.GetExtension(fileName).ToLower();
            switch (ext)
            {
                case ".f":
                case ".f&":
                case ".f+":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsServerSide(this FileContext fc)
        {
            switch (fc)
            {
                case FileContext.ServerTrigger:
                case FileContext.ServerClass:
                case FileContext.ServerProgram:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsClientSide(this FileContext fc)
        {
            switch (fc)
            {
                case FileContext.ClientTrigger:
                case FileContext.ClientClass:
                case FileContext.GatewayProgram:
                    return true;
                default:
                    return false;
            }
        }

        public static ServerContext ToServerContext(this FileContext fc)
        {
            switch (fc)
            {
                case FileContext.ServerTrigger:
                case FileContext.ServerClass:
                case FileContext.ServerProgram:
                    return ServerContext.Server;

                case FileContext.ClientTrigger:
                case FileContext.ClientClass:
                case FileContext.GatewayProgram:
                    return ServerContext.Client;

                default:
                    return ServerContext.Neutral;
            }
        }
    }
}
