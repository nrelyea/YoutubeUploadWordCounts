using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace YoutubeDataScraping
{
    class TitleWordCounts
    {
        static void Main(string[] args)
        {
            string URL = "https://www.youtube.com/user/TheCaptainKickass";

            List<string> videoTitles = ReadVideoTitles(URL);

            Hashtable wordsTable = BuildWordsTable(videoTitles);

            List<WordObj> sortedWordCountList = SortWordsTable(wordsTable);

            foreach(WordObj o in sortedWordCountList)
            {
                Console.WriteLine("{0}: {1}", o.Word, o.Count);
            }

        }

        static List<string> ReadVideoTitles(string channelURL)
        {
            List<string> videoTitleList = new List<string> { };

            int videoCount = 0;

            try
            {
                var yt = new YouTubeService(new BaseClientService.Initializer() { ApiKey = "AIzaSyAeld26NsXf9QnSEzA1u-A4JWHGS_WHOc4" });
                var channelsListRequest = yt.Channels.List("contentDetails");

                //determine user / channel identifier to be used
                string channelIdentifier = "";
                for(int i = channelURL.Length - 2; i > 0; i--)
                {
                    if(channelURL[i] == '/')
                    {
                        channelIdentifier = channelURL.Substring(i + 1);
                        break;
                    }
                }

                // if channelName given is actually a channel ID, set it as the ID
                if(channelURL.Contains("/channel/"))
                {
                    channelsListRequest.Id = channelIdentifier;
                }
                else
                {
                    channelsListRequest.ForUsername = channelIdentifier;
                }

                Console.WriteLine("Executing request...");
                var channelsListResponse = channelsListRequest.Execute();
                foreach (var channel in channelsListResponse.Items)
                {
                    var uploadsListId = channel.ContentDetails.RelatedPlaylists.Uploads;
                    var nextPageToken = "";
                    while (nextPageToken != null)
                    {
                        var playlistItemsListRequest = yt.PlaylistItems.List("snippet");
                        playlistItemsListRequest.PlaylistId = uploadsListId;
                        playlistItemsListRequest.MaxResults = 50;
                        playlistItemsListRequest.PageToken = nextPageToken;
                        // Retrieve the list of videos uploaded to the authenticated user's channel.  
                        var playlistItemsListResponse = playlistItemsListRequest.Execute();

                        videoCount += playlistItemsListResponse.Items.Count;
                        Console.Clear();
                        Console.WriteLine("Uploads retrieved: " + videoCount);

                        foreach (var playlistItem in playlistItemsListResponse.Items)
                        {
                            //Console.Write("Video ID ={0} ", "https://www.youtube.com/embed/" + playlistItem.Snippet.ResourceId.VideoId);
                            //Console.WriteLine("Video Title ={0} ", playlistItem.Snippet.Title);  
                            //Console.Write("Video Descriptions = {0}", playlistItem.Snippet.Description);  
                            //Console.WriteLine("Video ImageUrl ={0} ", playlistItem.Snippet.Thumbnails.High.Url);  
                            //Console.WriteLine("----------------------");  

                            videoTitleList.Add(playlistItem.Snippet.Title);
                        }
                        nextPageToken = playlistItemsListResponse.NextPageToken;


                    }
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Some exception occured\n" + e);
            }

            return videoTitleList;
        }

        static Hashtable BuildWordsTable(List<string> videoTitleList)
        {
            Console.WriteLine("Building word table...");

            Hashtable table = new Hashtable();

            foreach (string title in videoTitleList)
            {
                string[] words = title.Split(' ');
                foreach(string word in words)
                {
                    string wordAdjusted = RemoveSpecialCharacters(word);

                    if(wordAdjusted.Length > 0)
                    {
                        if (table.ContainsKey(wordAdjusted))
                        {
                            table[wordAdjusted] = (int)table[wordAdjusted] + 1;
                        }
                        else
                        {
                            table.Add(wordAdjusted, 1);
                        }
                    }
                }
            }

            return table;
        }

        static string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
        }

        static List<WordObj> SortWordsTable(Hashtable table)
        {
            Console.WriteLine("Sorting words / count list...");

            List<WordObj> objList = new List<WordObj> { };

            foreach (object key in table.Keys)
            {
                //Console.WriteLine(String.Format("{0}: {1}", key, table[key]));
                objList.Add(new WordObj((string)key, (int)table[key]));
            }

            List<WordObj> SortedList = objList.OrderByDescending(o => o.Count).ToList();

            return SortedList;
        }
    }

    public class WordObj
    {
        public string Word { get; set; }
        public int Count { get; set; }

        public WordObj(string word, int count)
        {
            Word = word;
            Count = count;
        }
    }


}
