using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GoogleAnalytics : MonoBehaviour {
	
	public string propertyID;
	public string defaultURL;
	
	public static GoogleAnalytics instance;
	
	private Hashtable requestParams = new Hashtable();
	private List<Hashtable> eventList= new List<Hashtable>();
	
	private string currentSessionStartTime;
	private string lastSessionStartTime;
	private string firstSessionStartTime;
	private int sessions;
	
	void Awake()
	{
		if(instance)
			DestroyImmediate(gameObject);
		else
		{
			DontDestroyOnLoad(gameObject);
			instance = this;
		}
	}
	
	void Start()
	{
        // Increment the number of times played
        IncrSessions();

		string screenResolution = Screen.width.ToString() + "x" + Screen.height.ToString();
		string buildNum  = "BuildNumber";
		string buildName = "BuildName";
		
		//Get the player prefs last time played and current time
		currentSessionStartTime = GetEpochTime().ToString();
		lastSessionStartTime = SavedLastSessionStartTime;
		firstSessionStartTime = SavedFirstSessionStartTime;
		sessions = NumSessions;
		
		requestParams["utmac"] = propertyID;
		requestParams["utmhn"] = defaultURL;
		requestParams["utmdt"] = buildNum;
		requestParams["utmp"]  = buildName;
		requestParams["utmfl"] = Application.unityVersion.ToString();	
		requestParams["utmsc"] = "24-bit";
		requestParams["utmsr"] = screenResolution;
		requestParams["utmwv"] = "5.3.8";
		requestParams["utmul"] = "en-us";
		
		// Set the last session start time
		SavedLastSessionStartTime = currentSessionStartTime;
	}
	
	public void Add(GAEvent gaEvent)
	{
		Hashtable urlParams = requestParams;
		
		urlParams["utmt"]  = GoogleTrackTypeToString( GoogleTrackType.GAEvent );
		urlParams["utmcc"] = CookieData();
		urlParams["utmn"]  = Random.Range(1000000000,2000000000).ToString();
		urlParams["utme"]  = gaEvent.ToUrlParamString();
		
		if (gaEvent.NonInteraction)
		{
			urlParams["utmni"] = 1;	
		}
		eventList.Add(urlParams);
	}
	
	public void Add(GALevel gaLevel)
	{
		Hashtable urlParams = requestParams;
		
		urlParams["utmt"]  = GoogleTrackTypeToString( GoogleTrackType.GALevel );
		urlParams["utmcc"] = CookieData();
		urlParams["utmn"]  = Random.Range(1000000000,2000000000).ToString();
		urlParams["utmp"]  = gaLevel.ToUrlParamString();
		eventList.Add(urlParams);
	}
	
	public void Add(GAUserTimer gaUserTimer)
	{
	 	// https://developers.google.com/analytics/devguides/collection/gajs/gaTrackingTiming
		Hashtable urlParams = requestParams;
		
		urlParams["utmt"] = GoogleTrackTypeToString( GoogleTrackType.GATiming );
		urlParams["utmn"] = Random.Range(1000000000,2000000000).ToString();
		urlParams["utmcc"] = CookieData();
		urlParams["utme"]  = gaUserTimer.ToUrlParamString();
		eventList.Add(urlParams);
	}
	
	public void Dispatch()
	{
		// Send the data to the Google Servers
		List<Hashtable> tmpDelete = new List<Hashtable>();
		foreach(Hashtable e in eventList)
		{
    		string urlParams = BuildRequestString(e);
			string url = "http://www.google-analytics.com/__utm.gif?" + urlParams;
			new WWW(url);
			
			Debug.Log(url);
			
			tmpDelete.Add(e);
		}
		
		foreach(Hashtable e in tmpDelete)
		{
			if (eventList.Contains(e))
			{
				eventList.Remove(e);	
			}
		}
	}
	
	private string GoogleTrackTypeToString(GoogleTrackType trackType)
	{
		switch(trackType)
		{
		case GoogleTrackType.GAEvent:
			return "event";
		case GoogleTrackType.GALevel:
			return "page";
		case GoogleTrackType.GATiming:
			return "event";
		default:
			return "page";
		}
	}
	
	private long DeviceIdentifier
	{
        get{ return Hash (SystemInfo.deviceUniqueIdentifier ); }
	}
	
	private int NumSessions
	{
		get{ return PlayerPrefs.GetInt("gaNumSessions"); }
	}
	
	private void IncrSessions()
	{
		int sessions = PlayerPrefs.GetInt("gaNumSessions");
		sessions += 1;
		PlayerPrefs.SetInt("gaNumSessions", sessions);
	}
	
	private string SavedFirstSessionStartTime
	{
		get{ if (PlayerPrefs.HasKey("gaFirstSessionStartTime"))
			{
				return PlayerPrefs.GetString("gaFirstSessionStartTime");
			}else{
				long currentTime = GetEpochTime();
				PlayerPrefs.SetString("gaFirstSessionStartTime", currentTime.ToString());
				PlayerPrefs.SetString("gaLastSessionStartTime", currentTime.ToString());
				return PlayerPrefs.GetString("gaFirstSessionStartTime");
			}
		}
	}
	
	private string SavedLastSessionStartTime
	{
		get{ return PlayerPrefs.GetString("gaLastSessionStartTime"); }
		set{ PlayerPrefs.SetString("gaLastSessionStartTime", value.ToString()); }
	}
	
	// Grab the cookie data for every event/pageview because it grabs the current time
	private string CookieData()
	{
		long currentTime  = GetEpochTime();
		long domainHash   = Hash(defaultURL);
		
		// __utma Identifies unique Visitors
		string _utma   = domainHash + "." + DeviceIdentifier + "." + firstSessionStartTime + "." + 
			lastSessionStartTime + "." + currentSessionStartTime + "." + sessions + WWW.EscapeURL(";") + WWW.EscapeURL("+");

		// __utmz Referral information in the cookie
		string cookieUTMZstr = "utmcsr" + WWW.EscapeURL("=") + "(direct)" + WWW.EscapeURL("|") + 
			"utmccn" + WWW.EscapeURL("=") + "(direct)" + WWW.EscapeURL("|") + 
			"utmcmd" + WWW.EscapeURL("=") + "(none)" + WWW.EscapeURL(";");
		
		string _utmz = domainHash + "." + currentTime + "." + sessions + ".1." + cookieUTMZstr;
		
		return "__utma" + WWW.EscapeURL("=") + _utma + "__utmz" + WWW.EscapeURL("=") + _utmz;
	}
	
	private string BuildRequestString(Hashtable urlParams)
	{
		List<string> args = new List<string>();
		foreach( string key in urlParams.Keys ) 
		{
			args.Add( key + "=" + urlParams[key] );	
		}
		return string.Join("&", args.ToArray());	
	}
	
	private long Hash(string url)
	{
		if(url.Length < 3) return Random.Range(10000000,99999999);
		
		int hash = 0;
		int hashCmp = 0;
		for(int urlLen=url.Length-1; urlLen>=0; urlLen--)
		{
			int charCode = (int)url[urlLen];
		    hash    = (hash<<6&268435455) + charCode + (charCode<<14);
		    hashCmp = hash&266338304;
		    hash    = hashCmp != 0 ? hash^hashCmp>>21 : hash;
		}
		return hash;
	}
	
	public long GetEpochTime() 
	{
		System.DateTime currentTime = System.DateTime.Now;
		System.DateTime epochStart  = System.Convert.ToDateTime("1/1/1970 0:00:00 AM");
		System.TimeSpan timeSpan    = currentTime.Subtract(epochStart);
		
		long epochTime = ((((((timeSpan.Days * 24) + timeSpan.Hours) * 60) + timeSpan.Minutes) * 60) + timeSpan.Seconds);
		
		return epochTime;
	}
}

public enum GoogleTrackType{
	GALevel,
	GAEvent,
	GATiming,
}

public class GALevel
{
	private string _page_name;
	
	public GALevel ()
	{
		_page_name = Application.loadedLevelName;	
	}
	
	public GALevel (string levelName)
	{
		_page_name = levelName;	
	}
	
	
	public string Level
	{
		get{ return _page_name; }
		set{ _page_name = value; }
	}
	
	public string ToUrlParamString()
	{
		if (Level == null)
		{
			throw new System.ArgumentException("GALevel: Please Specify a Level Name");	
		}
		return Level;
	}
}

public class GAEvent
{
	private string _category;
	private string _action;
	private string _opt_label;
	private int _opt_value = -1;
	private bool _opt_noninteraction = false;
	
	public GAEvent(string category, string action)
	{
		Category = category;
		Action   = action;
	}
	
	public GAEvent(string category, string action, string label)
	{
		Category = category;
		Action   = action;
		Label    = label;
	}
	
	public GAEvent(string category, string action, string label, int opt_value)
	{
		Category = category;
		Action   = action;
		Label    = label;
		Value    = opt_value;
	}
	
	public string Category
	{
		get{ return _category;  }
		set{ _category = value; }
	}
	
	public string Action
	{
		get{ return _action;}
		set{ _action = value;}
	}
	
	public string Label
	{
		get{ return _opt_label; }
		set{ _opt_label = value; }
	}
	
	public int Value
	{
		get{ return _opt_value; }
		set{ _opt_value = value; }
	}
	
	public bool NonInteraction
	{
		get{ return _opt_noninteraction;}
		set{ _opt_noninteraction = value; }
	}
	
	public string ToUrlParamString()
	{
		//"5(<category>*<action>*<label>*<value>)"
		string utme = "5(";
		utme += Category;
		utme += "*" + Action;
		if (Category == null || Action == null)
		{
			throw new System.ArgumentException("GAEvent: Category and Action must be specified");	
		}
		if (Label != null)
		{
			utme += "*" + Label;
		}
		
		if (Value != -1)
		{
			utme += "*" + Value;
		}
		
		utme += ")";
			
		return utme;	
	}
}

public class GAUserTimer
{
	private string _category;
	private string _variable;
	private string _label;
	
	private long _startTime;
	private long _stopTime;
	
	public GAUserTimer(string category, string variable)
	{
		Category = category;
		Variable = variable;
	}
	
	public GAUserTimer(string category, string variable, string label)
	{
		Category = category;
		Variable = variable;
		Label    = label;
	}
	
	public string Category
	{
		get{ return _category; }
		set{ _category = value; }
	}
	
	public string Variable
	{
		get{ return _variable; }
		set{ _variable = value; }
	}
	
	public string Label
	{
		get{ return _label; }
		set{ _label = value; }
	}
	
	public void Start(){
		_startTime = GoogleAnalytics.instance.GetEpochTime();
	}
	
	public void Stop()
	{
		_stopTime  = GoogleAnalytics.instance.GetEpochTime();
	}
	
	private long ElapsedTime()
	{
		if (_startTime == 0 || _stopTime == 0)
		{
			throw new System.ArgumentException("To get the elapsed time please specify the start and end time");	
		}
		return (_stopTime - _startTime) * 1000;
	}
	
	public string ToUrlParamString()
	{
		string utme = "14(90!";
		utme +=  Variable;
		utme += "*" + Category;
		utme += "*" + ElapsedTime().ToString();
		if (Label != null)
		{
			utme += "*" + Label;
		}
		utme += ")";
		utme += "(90!" + ElapsedTime().ToString() + ")";
		
		return utme;
	}
	
}