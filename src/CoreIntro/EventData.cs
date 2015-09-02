namespace CoreIntro
{
	public class EventData
	{
		public string EventName { get; set; }
		public string Level { get; set; }
		public string Channel { get; set; }
		public string Keywords { get; set; }
		public string ParameterList { get; set; }
		public string WriteEventParams { get; set; }
		public string Opcode { get; set; }
		public string Task { get; set; }
		public string Message { get; set; }
		
		public override string ToString()
		{
			return string.Format(@"        public bool {0}IsEnabled()
        {{
            return base.IsEnabled({2}, {4}, {3});
        }}

        [Event(EventIds.{1}, Level = {2}, Channel = {3}, Opcode = {7}{8}, 
            Keywords = {4}, 
            Message = ""{9}"")]
        public void {0}({5})
        {{
            WriteEvent(EventIds.{1}{6});
        }}
",
				EventName,
				EventName,
				Level,
				Channel,
				Keywords != string.Empty ? Keywords : "EventKeywords.None",
				ParameterList,
				!string.IsNullOrEmpty(WriteEventParams) ? ", " + WriteEventParams : "",
				Opcode,
				Task,
				Message);

		}
	}
}