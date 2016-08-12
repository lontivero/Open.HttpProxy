using System.Globalization;
using System.Text;

namespace Open.HttpProxy.Utils
{
	static class Html
	{
		public static string Encode(string content)
		{
			var len = content.Length;
			var stringBuilder = new StringBuilder(len);

			for (var i = 0; i < len; i++)
			{
				var c = content[i];

				switch (c)
				{
					case '"':
						stringBuilder.Append("&quot;");
						break;
					case '&':
						stringBuilder.Append("&amp;");
						break;
					case '<':
						stringBuilder.Append("&lt;");
						break;
					case '>':
						stringBuilder.Append("&gt;");
						break;
					default: {
						if (c > '\u009f')
						{
							stringBuilder.Append("&#");
							stringBuilder.Append(((int) c).ToString(NumberFormatInfo.InvariantInfo));
							stringBuilder.Append(";");
						}
						else
						{
							stringBuilder.Append(c);
						}
						break;
					}
				}
			}
			return stringBuilder.ToString();
		}
	}
}
