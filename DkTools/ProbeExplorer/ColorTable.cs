using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.ProbeExplorer
{
	class ColorTable
	{
		public static void GenerateColorSampleFile(string fileName)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<!DOCTYPE html><html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\">");
			sb.AppendLine("<head><meta charset=\"utf-8\"/>");
			sb.AppendLine("<title></title>");
			sb.AppendLine("<style>");
			sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif }");
			sb.AppendLine(".colorLabel { margin-left: 10px; }");
			sb.AppendLine(".colorSample { display: inline-block; width: 100px; height: 20px; border: 1px solid black; }");
			sb.AppendLine(".noMatch { display: none; }");
			sb.AppendLine(".filterPanel { margin-bottom: 10px; }");
			sb.AppendLine("</style>");
			sb.AppendLine("</head><body>");
			sb.AppendLine("<div class=\"filterPanel\"><input type=\"text\" id=\"filterText\" /></div>");

			foreach (var prop in typeof(EnvironmentColors).GetProperties())
			{
				if (prop.PropertyType == typeof(ThemeResourceKey))
				{
					var value = prop.GetValue(null) as ThemeResourceKey;
					if (value != null)
					{
						var color = VSColorTheme.GetThemedColor(value);
						sb.AppendLine($"<div class=\"colorLine\"><span class=\"colorSample\" style=\"background: #{color.R:X2}{color.G:X2}{color.B:X2}\"></span><span class=\"colorLabel\">{prop.Name}</span></div>");
					}
				}
			}

			sb.AppendLine(@"<script>
document.querySelector('#filterText').addEventListener('keyup', function () {
    let searchPattern = this.value.toLowerCase().split(' ');
    console.log(searchPattern);

    let lines = document.querySelectorAll('.colorLine');
    for (var i = 0, ii = lines.length; i < ii; i++)
    {
        let labels = lines[i].getElementsByClassName('colorLabel');
        let content = labels[0].innerHTML.toLowerCase();
        let match = false;
        for (var j = 0, jj = searchPattern.length; j < jj; j++)
        {
            if (searchPattern[j] !== '' && content.indexOf(searchPattern[j]) >= 0)
            {
                match = true;
                break;
            }
        }

        if (match)
        {
            if (lines[i].classList.contains('noMatch')) lines[i].classList.remove('noMatch');
        }
        else
        {
            if (!lines[i].classList.contains('noMatch')) lines[i].classList.add('noMatch');
        }
    }
});
</script>");

			sb.AppendLine("</body></html>");
			System.IO.File.WriteAllText(fileName, sb.ToString());
		}
	}
}
