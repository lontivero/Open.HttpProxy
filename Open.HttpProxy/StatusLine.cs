using System;

namespace Open.HttpProxy
{
	public class StatusLine
	{
		public static StatusLine Parse(string line)
		{
			var ifs = line.IndexOf(' ');
			var ils = line.IndexOf(' ', ifs+1);
			var version = ProtocolVersion.Parse(line.Substring(0, ifs));
			var code = line.Substring(ifs + 1, ils - ifs - 1);
			var description = line.Substring(ils + 1);
			return new StatusLine(version, code, description);
		}

		public StatusLine(ProtocolVersion version, string code, string description)
		{
			Code = code;
			Description = description;
			Version = version;
		}

		public string Code
		{
			get;
			set;
		}

		public string Description
		{
			get;
			set;
		}

		public ProtocolVersion Version
		{
			get;
			set;
		}

		public override string ToString()
		{
			return $@"{Version} {Code} {Description}";
		}
	}
}