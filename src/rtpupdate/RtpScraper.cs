using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Collections;
using System.Data.SqlTypes;

namespace rtpupdate
{
    public class RtpScraper
    {
        rtpdbDataContext db;
        public RtpScraper(rtpdbDataContext DB)
        {
            this.db = DB;
        }
        public void execute()
        {
            // iterate over list of stations where last-harvest dates are less than today, ordered by last-harvest date ascending
            foreach(var station in db.Stations.Where(s => s.LastHarvestDate.GetValueOrDefault(SqlDateTime.MinValue.Value).Date < DateTime.Now.Date).OrderBy(s => s.LastHarvestDate).ToList())
            {
                try
                {
                    // foreach in list, process station (i.e. start with least last-accessed date)
                    var finalYear = DateTime.Now;
                    var currentYear = station.LastHarvestDate.GetValueOrDefault(DateTime.MinValue);
                    while (currentYear.Year <= finalYear.Year)
                    {
                        var values = extractValues(station.downloadData(currentYear), currentYear);
                        foreach (var v in values)
                        {
                            v.StationID = station.StationID;
                            db.DataValues.InsertOnSubmit(v);
                        }

                        currentYear = currentYear.AddYears(1);
                    }
                    station.LastHarvestDate = finalYear;
                    db.SubmitChanges();
                }
                catch
                {
                    db.GetChangeSet().Updates.Clear(); // clear updates
                    db.GetChangeSet().Inserts.Clear(); // clear inserts
                    db.GetChangeSet().Deletes.Clear(); // clear deletes
                    continue;
                }
            }
        }
        
        public int NthIndexOf(string s, string match, int n)
        {
            int i = 1;
            int index = 0;

            while (i <= n && (index = s.IndexOf(match, index + 1)) != -1)
            {
                if (i == n)
                    return index;

                i++;
            }

            return -1;
        }

        /// <summary>
        /// Extracts the values.
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <param name="startDate">The date from which to start adding data.</param>
        /// <returns></returns>
        public IEnumerable<DataValue> extractValues(string html, DateTime startDate)
        {
            foreach (string line in new LineReader(() => new StringReader(html)))
            {
                DataValue value;
                try
                {
                    value = new DataValue();
                    
                    value.Date = DateTime.Parse(line.Substring(0, 8));
                    // Should we include this value?
                    if(value.Date < startDate)
                    {
                        throw new ApplicationException();
                    }
                    
                    var stringWithValues = line.Substring(NthIndexOf(line, ":", 2) + 1);
                    value.High = (int?) parseValueInput(stringWithValues.Substring(0, stringWithValues.IndexOf("/")));
                    
                    stringWithValues = stringWithValues.Substring(stringWithValues.IndexOf("/") + 1);
                    value.Low = (int?) parseValueInput(stringWithValues.Substring(0, stringWithValues.IndexOf("/")));
                    
                    stringWithValues = stringWithValues.Substring(stringWithValues.IndexOf("/") + 1);
                    value.Rainfall = parseValueInput(stringWithValues.Substring(0, stringWithValues.IndexOf("/")));
                }
                catch
                {
                    value = null;
                    continue;
                }
                yield return value;
            }
        }

        public double? parseValueInput(string input)
        {
            input = input.Trim();
            if(input == "M") // missing
            {
                return null;
            }
            if(input == "T") // traces
            {
                return -999;
            }
            return double.Parse(input);
        }

    }
    public partial class Station
    {
        public string downloadData(DateTime whichYear)
        {
            var year = whichYear.ToString("yy"); // 2001 => 01
            var url = "http://www.wrh.noaa.gov/sgx/obs/rtp/" + this.StationURL.Replace("%s", year);
            var client = new WebClient();
            return client.DownloadString(url);
        }
    }

    /// <summary>
    /// Reads a data source line by line. The source can be a file, a stream,
    /// or a text reader. In any case, the source is only opened when the
    /// enumerator is fetched, and is closed when the iterator is disposed.
    /// From MiscUtil: http://www.yoda.arachsys.com/csharp/miscutil/
    /// </summary>
    public sealed class LineReader : IEnumerable<string>
    {
        /// <summary>
        /// Means of creating a TextReader to read from.
        /// </summary>
        readonly Func<TextReader> dataSource;

        /// <summary>
        /// Creates a LineReader from a stream source. The delegate is only
        /// called when the enumerator is fetched. UTF-8 is used to decode
        /// the stream into text.
        /// </summary>
        /// <param name="streamSource">Data source</param>
        public LineReader(Func<Stream> streamSource)
            : this(streamSource, Encoding.UTF8)
        {
        }

        /// <summary>
        /// Creates a LineReader from a stream source. The delegate is only
        /// called when the enumerator is fetched.
        /// </summary>
        /// <param name="streamSource">Data source</param>
        /// <param name="encoding">Encoding to use to decode the stream
        /// into text</param>
        public LineReader(Func<Stream> streamSource, Encoding encoding)
            : this(() => new StreamReader(streamSource(), encoding))
        {
        }

        /// <summary>
        /// Creates a LineReader from a filename. The file is only opened
        /// (or even checked for existence) when the enumerator is fetched.
        /// UTF8 is used to decode the file into text.
        /// </summary>
        /// <param name="filename">File to read from</param>
        public LineReader(string filename)
            : this(filename, Encoding.UTF8)
        {
        }

        /// <summary>
        /// Creates a LineReader from a filename. The file is only opened
        /// (or even checked for existence) when the enumerator is fetched.
        /// </summary>
        /// <param name="filename">File to read from</param>
        /// <param name="encoding">Encoding to use to decode the file
        /// into text</param>
        public LineReader(string filename, Encoding encoding)
            : this(() => new StreamReader(filename, encoding))
        {
        }

        /// <summary>
        /// Creates a LineReader from a TextReader source. The delegate
        /// is only called when the enumerator is fetched
        /// </summary>
        /// <param name="dataSource">Data source</param>
        public LineReader(Func<TextReader> dataSource)
        {
            this.dataSource = dataSource;
        }

        /// <summary>
        /// Enumerates the data source line by line.
        /// </summary>
        public IEnumerator<string> GetEnumerator()
        {
            using (TextReader reader = dataSource())
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        /// <summary>
        /// Enumerates the data source line by line.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
