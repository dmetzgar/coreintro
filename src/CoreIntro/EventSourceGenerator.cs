namespace CoreIntro
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Xml;
	
	public class EventSourceGenerator
	{
		public const string EventSchemaUrl = "http://schemas.microsoft.com/win/2004/08/events";
		
		private readonly XmlDocument doc;
		private readonly XmlNamespaceManager nsmgr;
		private readonly string EventSourceName;

		public Dictionary<string, string> Keywords { get; private set; }
		public Dictionary<string, string> Tasks { get; private set; }
		public Dictionary<string, string> Opcodes { get; private set; }
		public Dictionary<string, int> EventIds { get; private set; }
		public List<EventData> Events { get; private set; }
		
		public EventSourceGenerator(string manifestPath, string eventSourceName)
		{
			doc = new XmlDocument();
			using (var stream = new FileStream(manifestPath, FileMode.Open))
			{
				doc.Load(stream);				
			}
			nsmgr = new XmlNamespaceManager(doc.NameTable);
			nsmgr.AddNamespace("d", EventSchemaUrl);
			EventSourceName = eventSourceName;
			Keywords = new Dictionary<string, string>();
			Tasks = new Dictionary<string, string>();
			Opcodes = new Dictionary<string, string>();
			EventIds = new Dictionary<string, int>();
			Events = new List<EventData>();
		}
		
		public void ParseDocument()
		{
			foreach (XmlElement keywordNode in doc.DocumentElement.SelectNodes("//d:provider/d:keywords/d:keyword", nsmgr))
			{
				Keywords.Add(keywordNode.GetAttribute("name"), keywordNode.GetAttribute("mask"));
			}
			
			foreach (XmlElement eventNode in doc.DocumentElement.SelectNodes("//d:provider/d:events/d:event", nsmgr).OfType<XmlElement>().OrderBy (xe => int.Parse(xe.GetAttribute("value"))))
			{
				string eventName = eventNode.GetAttribute("symbol");
				int eventId = int.Parse(eventNode.GetAttribute("value"));
				EventIds.Add(eventName, eventId);
				
				XmlElement templateNode = doc.DocumentElement.SelectSingleNode(string.Format(@"//d:templates/d:template[@tid=""{0}""]", eventNode.GetAttribute("template")), nsmgr) as XmlElement;
				StringBuilder parameterList = new StringBuilder();
				StringBuilder writeEventParams = new StringBuilder();
				foreach (XmlElement parameter in templateNode.SelectNodes("d:data", nsmgr))
				{
					var outType = parameter.GetAttribute("outType").Replace("xs:", "");
					var paramName = parameter.GetAttribute("name");
					var eventParam = paramName;
					switch (outType)
					{
						case "GUID": outType = "Guid"; break;
						case "dateTime": outType = "DateTime"; break;
						case "unsignedByte": outType = "byte"; break;
						case "unsignedLong": outType = "ulong"; break;
					}
					if (paramName.IndexOf("AppDomain", StringComparison.OrdinalIgnoreCase) > -1)
						continue;
					if (parameterList.Length > 0) 
					{
						parameterList.Append(", ");
						writeEventParams.Append(", ");
					}

					parameterList.Append(outType);
					parameterList.Append(" ");
					parameterList.Append(paramName);
					writeEventParams.Append(eventParam);
				}

				string opcode = eventNode.GetAttribute("opcode");
				string task = eventNode.GetAttribute("task");
				XmlElement taskNode = doc.DocumentElement.SelectSingleNode(string.Format(@"//d:tasks/d:task[@name=""{0}""]", task), nsmgr) as XmlElement;
				if (taskNode != null)
				{
					Tasks[task] = taskNode.GetAttribute("value");
					foreach (XmlElement opcodeNode in taskNode.SelectNodes(@"d:opcodes/d:opcode", nsmgr))
					{
						Opcodes[task + opcodeNode.GetAttribute("name")] = opcodeNode.GetAttribute("value");
					}
				}
	
				if (opcode.StartsWith("win:"))
					opcode = "EventOpcode." + opcode.Replace("win:", "");
				else
					opcode = "Opcodes." + (taskNode != null ? task + opcode : opcode);
				string channel = eventNode.GetAttribute("channel");
				switch (channel) 
				{
					case "DEBUG_CHANNEL": channel = "EventChannel.Debug"; break;
					case "ANALYTIC_CHANNEL": channel = "EventChannel.Analytic"; break;
					case "OPERATIONAL_CHANNEL": channel = "EventChannel.Operational"; break;
					case "ADMIN_CHANNEL": channel = "EventChannel.Admin"; break;
				}
				string keywords = eventNode.GetAttribute("keywords");
				if (string.IsNullOrWhiteSpace(keywords))
					keywords = "";
				else
					keywords = string.Join(" | ", eventNode.GetAttribute("keywords").Split(' ').Select (n => "Keywords." + n));
				string msgPath = eventNode.GetAttribute("message");
				msgPath = msgPath.Replace("$(string.", "").Replace(")", "");
				XmlElement msgNode = doc.DocumentElement.SelectSingleNode(string.Format(@"//d:resources/d:stringTable/d:string[@id=""{0}""]", msgPath), nsmgr) as XmlElement;
				string value = msgNode.GetAttribute("value");
				for (int i = 19; i > 0; i--)
					value = value.Replace("%" + i, "{" + (i - 1) + "}");
				Events.Add(new EventData() {
					EventName = eventName,
					Level = eventNode.GetAttribute("level").Replace("win:", "EventLevel."),
					Channel = channel,
					Keywords = keywords,
					ParameterList = parameterList.ToString(),
					WriteEventParams = writeEventParams.ToString(),
					Opcode = opcode,
					Task = taskNode != null ? ", Task = Tasks." + task : "",
					Message = value
				});
			}
		}
		
		private string GetEventIdsString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(@"        public class EventIds
        {");
			foreach (var id in EventIds.OrderBy (e => e.Value))
			{
				sb.AppendFormat("            public const int {0} = {1};{2}", id.Key, id.Value, Environment.NewLine);
			}
			sb.AppendLine(@"        }");
			return sb.ToString();
		}
		
		private string GetTasksString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(@"        public class Tasks
        {");
			foreach (var task in Tasks.OrderBy (e => e.Value))
			{
				sb.AppendFormat("            public const EventTask {0} = (EventTask){1};{2}", task.Key, task.Value, Environment.NewLine);
			}
			sb.AppendLine(@"        }");
			return sb.ToString();
		}
		
		private string GetOpcodesString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(@"        public class Opcodes
        {");
			foreach (var opcode in Opcodes.OrderBy (e => e.Value))
			{
				sb.AppendFormat("            public const EventOpcode {0} = (EventOpcode){1};{2}", opcode.Key, opcode.Value, Environment.NewLine);
			}
			sb.AppendLine(@"        }");
			return sb.ToString();
		}
		
		private string GetKeywordsString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(@"        public class Keywords
        {");
			foreach (var keyword in Keywords)
			{
				sb.AppendFormat("            public const EventKeywords {0} = (EventKeywords){1};{2}", keyword.Key, keyword.Value, Environment.NewLine);
			}
			sb.AppendLine(@"        }");
			return sb.ToString();
		}
		
		public string GetEventSourceString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat(@"using System.Diagnostics.Tracing;

    [EventSource]
    public sealed class {0} : EventSource
    {{
        public static {0} Instance = new {0}();

",
				EventSourceName);
			foreach (var eventData in Events)
			{
				sb.AppendLine(eventData.ToString());
			}
			sb.AppendLine(@"        #region Keywords / Tasks / Opcodes");
			sb.AppendLine();
			sb.AppendLine(GetEventIdsString());
			sb.AppendLine(GetTasksString());
			sb.AppendLine(GetOpcodesString());
			sb.AppendLine(GetKeywordsString());
			sb.AppendLine(@"        #endregion");
			sb.AppendLine(@"    }");
			
			return sb.ToString();
		}
	}
}