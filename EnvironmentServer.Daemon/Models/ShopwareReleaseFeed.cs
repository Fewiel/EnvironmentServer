using System.Collections.Generic;
using System.Xml.Serialization;

namespace EnvironmentServer.Daemon.Models;

// using System.Xml.Serialization;
// XmlSerializer serializer = new XmlSerializer(typeof(Releases));
// using (StringReader reader = new StringReader(xml))
// {
//    var test = (Releases)serializer.Deserialize(reader);
// }

[XmlRoot(ElementName = "en")]
public class En
{
	[XmlElement(ElementName = "changelog")]
	public string Changelog { get; set; }

	[XmlElement(ElementName = "important_changes")]
	public string ImportantChanges { get; set; }
}

[XmlRoot(ElementName = "locales")]
public class Locales
{
	[XmlElement(ElementName = "en")]
	public En En { get; set; }
}

[XmlRoot(ElementName = "release")]
public class Release
{
	[XmlElement(ElementName = "type")]
	public string Type { get; set; }

	[XmlElement(ElementName = "release_date")]
	public string ReleaseDate { get; set; }

	[XmlElement(ElementName = "locales")]
	public Locales Locales { get; set; }

	[XmlElement(ElementName = "download_link_install")]
	public string DownloadLinkInstall { get; set; }

	[XmlElement(ElementName = "download_link_update")]
	public string DownloadLinkUpdate { get; set; }

	[XmlElement(ElementName = "version")]
	public string Version { get; set; }

	[XmlElement(ElementName = "version_text")]
	public string VersionText { get; set; }

	[XmlElement(ElementName = "minimum_version")]
	public string MinimumVersion { get; set; }

	[XmlElement(ElementName = "public")]
	public int Public { get; set; }

	[XmlElement(ElementName = "rc")]
	public int Rc { get; set; }
}

[XmlRoot(ElementName = "releases")]
public class ShopwareReleaseFeed
{

	[XmlElement(ElementName = "release")]
	public List<Release> Releases { get; set; }
}