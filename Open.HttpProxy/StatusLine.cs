using System;

namespace Open.HttpProxy
{
	public class StatusLine
	{
		public StatusLine(string line)
		{
			var ifs = line.IndexOf(' ');
			var ils = line.LastIndexOf(' ');
			Version = line.Substring(0, ifs);
			Code = line.Substring(ifs + 1, ils - ifs - 1);
			Description = line.Substring(ils + 1);
		}

		public StatusLine(string version, string code, string description)
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

		public string Version
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