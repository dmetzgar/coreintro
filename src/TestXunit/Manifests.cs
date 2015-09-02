namespace TestXunit
{
	using CoreIntro;
	using Xunit;
	
	public class Manifests
	{
		private EventSourceGenerator eventSource;
		
		public Manifests()
		{
			eventSource = new EventSourceGenerator(@"..\..\TestFiles\SampleManifest.man", "SampleEventSource");
			eventSource.ParseDocument();
		}
		
		[Fact]
		public void EventsFound()
		{
			Assert.True(eventSource.Events.Count > 0);
		}
		
		[Theory]
		[InlineData("Troubleshooting")]
		[InlineData("Infrastructure")]
		public void KeywordExists(string keyword)
		{
			Assert.True(eventSource.Keywords.ContainsKey(keyword));
		}
	}
}