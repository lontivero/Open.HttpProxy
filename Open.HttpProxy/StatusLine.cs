namespace Open.HttpProxy
{
    public class StatusLine
    {
        public StatusLine(string code, string description)
        {
            Code = code;
            Description = description;
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

        public string ResponseLine
        {
            get
            {
                return string.Format("HTTP/1.1 {0} {1}", Code, Description);
            }
        }
    }
}