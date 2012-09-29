using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaskScheduler;
using System.Threading;

namespace rtpupdate
{
    public class Program
    {
        /// <summary>
        /// Start point.
        /// </summary>
        /// <param name="args">The args.</param>
        static void Main(string[] args)
        {
            verifyScheduledTask();
            Console.WriteLine("Running...");
            new RtpScraper(new rtpdbDataContext()).execute();
            Console.WriteLine("Done!");
            Thread.Sleep(5000);
        }

        /// <summary>
        /// Verifies whether the scheduled task exists, and if not, creates the scheduled task. See http://stackoverflow.com/a/2490142/130164.
        /// </summary>
        static void verifyScheduledTask()
        {
            //Get a ScheduledTasks object for the local computer.
            var st = new ScheduledTasks();

            // Create a task
            Task t;
            try
            {
                t = st.CreateTask("rtpupdate");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Scheduled task already set up.");
                return;
            }

            // Fill in the program info
            t.ApplicationName = System.Reflection.Assembly.GetExecutingAssembly().Location; // "chkdsk.exe"
            t.Parameters = ""; // "d: /f"
            t.Comment = "Retrieves RTP data from NOAA for San Diego stations."; // "Checks and fixes errors on D: drive"

            Console.WriteLine(@"Enter username in the form THEDOMAIN\TheUser.");
            var username = Console.ReadLine();
            Console.WriteLine("Enter password.");
            var password = Console.ReadLine();

            // Set the account under which the task should run.
            t.SetAccountInformation(username, password); //t.SetAccountInformation(@"THEDOMAIN\TheUser", "HisPasswd");

            // Declare that the system must have been idle for ten minutes before 
            // the task will start
            t.IdleWaitMinutes = 10;

            // Allow the task to run for no more than 2 hours, 30 minutes.
            t.MaxRunTime = new TimeSpan(2, 30, 0);

            // Set priority to only run when system is idle.
            //t.Priority = System.Diagnostics.ProcessPriorityClass.Idle; // this option never worked for me

            // Create a trigger to start the task every Sunday at 6:30 AM.
            t.Triggers.Add(new DailyTrigger(0, 30)); //t.Triggers.Add(new WeeklyTrigger(6, 30, DaysOfTheWeek.Sunday));

            // Save the changes that have been made.
            t.Save();
            // Close the task to release its COM resources.
            t.Close();
            // Dispose the ScheduledTasks to release its COM resources.
            st.Dispose();
        }

    }
}
