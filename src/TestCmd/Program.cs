namespace TestCmd
{
	using CoreIntro;
	using System;
	
	public class Program
	{
		public void Main(string[] args)
		{
			var eventSource = new EventSourceGenerator(@"..\..\TestFiles\SampleManifest.man", "SampleEventSource");
			eventSource.ParseDocument();
			Console.WriteLine(eventSource.GetEventSourceString());
		}
	}
}