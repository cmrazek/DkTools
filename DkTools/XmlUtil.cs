using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace DkTools
{
	public static class XmlUtil
	{
		public static string Serialize(object obj)
		{
			try
			{
				var sb = new StringBuilder();
				var xmlSettings = new XmlWriterSettings { OmitXmlDeclaration = true };
				using (var xmlWriter = XmlWriter.Create(sb, xmlSettings))
				{
					var serializer = new XmlSerializer(obj.GetType());
					serializer.Serialize(xmlWriter, obj);

					return sb.ToString();
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
				return string.Empty;
			}
		}

		public static void SerializeToFile(object obj, string fileName, bool indent)
		{
			using (var memStream = new MemoryStream())
			{
				var xmlSettings = new XmlWriterSettings();
				xmlSettings.Indent = indent;

				var xmlWriter = XmlWriter.Create(memStream, xmlSettings);
				var serializer = new XmlSerializer(obj.GetType());
				serializer.Serialize(xmlWriter, obj);

				var buf = new byte[memStream.Length];
				memStream.Seek(0, SeekOrigin.Begin);
				memStream.Read(buf, 0, (int)memStream.Length);

				File.WriteAllBytes(fileName, buf);
			}
		}

		public static T Deserialize<T>(string xml)
		{
			try
			{
				var stringReader = new StringReader(xml);
				try
				{
					using (var xmlReader = XmlReader.Create(stringReader))
					{
						stringReader = null;
						var serializer = new XmlSerializer(typeof(T));
						return (T)serializer.Deserialize(xmlReader);
					}
				}
				finally
				{
					if (stringReader != null) stringReader.Dispose();
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
				return default(T);
			}
		}

		public static T DeserializeFromFile<T>(string fileName)
		{
			using (var reader = new StreamReader(fileName))
			{
				var serializer = new XmlSerializer(typeof(T));
				return (T)serializer.Deserialize(reader);
			}
		}
	}
}
