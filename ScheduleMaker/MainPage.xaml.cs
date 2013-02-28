using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml;
using System.Windows.Browser;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Threading;
using ScheduleMaker.Entity;
using ScheduleMaker.Factory;
using System.Text.RegularExpressions;

//For debug
using System.Diagnostics;
using System.Text;

namespace ScheduleMaker
{
    public partial class MainPage : UserControl
    {
        school allClasses;                      //all the school's departments/classes/sections
        List<departmentClass> foundClasses = new List<departmentClass>();     //all the classes we've found by the search.
        List<Class> classes;                    //A list of all the classes
        private int classesIndex = 0;           //An index that represents the next free space in the classes list.
        List<Course> possibleSections;          //A list of sections that are about to be added to the scheduling
        int totalClassCount = 0;                //The total number of classes
        List<List<Course>> confirmedSections;   //A list of lists of courses grouped by schedule grid
        int tabCount = 0;                       //A number used to label the tabs.
        int tbaCount = 0;                       //The number of classes that have times of TBA. We need a count to track position.
        int secondsRemaining = 0;               //Seconds remaining until bitstrings are done.
        BackgroundWorker counterDown;           //The background worker that updates the timeleft label.
        Dictionary<Grid, List<Course>> sectionsInGrid;      //The sections that are in each grid.

        MyStopwatch testTimer;
        float averageTime = 0f;
        float sumAverageTime = 0f;
        byte testCounter = 0;

        /* Constructor.
         * 
         * Here we start all the GUI stuff.
         */
        public MainPage()
        {
            InitializeComponent();
            allClasses = (school)SchoolDataFactory.retrieveFromResourceXML(typeof(school), "classdata.xml");
            btnAddToList.IsEnabled = false;
            btnRemoveFromList.IsEnabled = false;
            btnSubmit.IsEnabled = false;
            confirmedSections = new List<List<Course>>();
            lstExclude.Visibility = System.Windows.Visibility.Collapsed;    //These controls need to be invisible at the start
            txtExclude.Visibility = System.Windows.Visibility.Collapsed;
            btnExclude.Visibility = System.Windows.Visibility.Collapsed;
            lblExcludeInfo.Visibility = System.Windows.Visibility.Collapsed;
        }

        /*  Called when the search class text box recieves focus.
         * 
         * Clears the textbox if it contains the default value.
         */ 
        private void TextBox_GotFocus_1(object sender, RoutedEventArgs e)
        {
            if (courseInput.Text == "Search courses...")    //This is the textbox's default text
            {
                courseInput.Text = "";
            }
        }

        /* Called when a key is pressed in the search class text box
         * 
         * Currently we're only interested when the enter button is pressed.
         *  In which case, we will begin a search for classes.
         */
        private void courseInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //our number finding regex string
                string pattern = @"(\w+)(\d+)";
                string departmentName = string.Empty;
                string courseNumber = string.Empty;
                //Make sure there's a chance for real input
                if (courseInput.Text.Trim() == "") { courseInput.Text = ""; return; }
                //Init data
                classes = new List<Class>();
                classesIndex = 0;
                classList.Items.Clear();
                btnAddToList.IsEnabled = false;
                //Clear any information in the information box
                classInfoBox.Text = "";
                //Figure out what department we're looking for
                string input = courseInput.Text;
                //Clear the input box
                courseInput.Text = "";

                input = Regex.Replace(input, @" ", string.Empty, RegexOptions.Multiline);
                
                var result = Regex.Match(input, pattern,RegexOptions.IgnorePatternWhitespace);
                if (result.Groups.Count > 1)
                {
                    departmentName = result.Groups[1].ToString();
                    courseNumber = result.Groups[2].ToString();
                }
                else
                {
                    return;
                }
                
                //Perform search
                findClasses(departmentName, courseNumber);
                // All the class data is loaded
                foreach (departmentClass found in foundClasses)
                {
                    classList.Items.Add(found);
                }
            }
        }

        private void findClasses(string departmentName, string courseNumber)
        {
            foreach (department dpt in allClasses.departments)
            {
                if (dpt.name == departmentName)
                {
                    foreach (departmentClass dptcls in dpt.classes)
                    {
                        if (dptcls.course.Contains(courseNumber))
                        {
                            foundClasses.Add(dptcls);
                        }
                    }
                }
            }
        }

        /* Called when the selection is changed on the searched class list.
         * 
         * Displays the correct class information and enables a button.
         */
        private void classList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //First, make sure items are being added. We don't care if they're being removed.
            if (e.RemovedItems.Count > 0 && e.AddedItems.Count < 1)
            {
                return;
            }
            //The class's information that we want to display is the class that was selected - the added item.
            departmentClass selectedClass = (departmentClass)classList.SelectedItem;

            classInfoBox.Text = selectedClass.getClassInfo();
            btnAddToList.IsEnabled = true;  //If there is an item selected, we can allow it to be added to the pending classes
        }

        /* This function is called when the add button is clicked.
         *
         * Moves the currently selected item from the searched classes to the pending classes.
         */
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            departmentClass selectedClass = (departmentClass)classList.SelectedItem;
            lstFinalClasses.Items.Add(selectedClass);
            classList.Items.Remove(selectedClass);
            classInfoBox.Text = "";                 //Make sure the information is clear, because we are deselecting
            btnSubmit.IsEnabled = true;             //Now that the final list is populated with at least one value,
                                                    // we can allow the submit button to be pushed.
            //If the list is empty, disable the button
            if (classList.SelectedItems.Count < 1) btnAddToList.IsEnabled = false;
        }

        /* Called when the selectionis changed on the final (pending) list.
         * 
         * Resets some GUI stuff. 
         */
        private void lstFinalClasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtExclude.Text = "";
            if (e.RemovedItems.Count > 0 && e.AddedItems.Count < 1)
            {
                if (lstFinalClasses.Items.Count < 1)
                {
                    btnSubmit.IsEnabled = false;
                }
                return;
            }
            departmentClass selectedClass = (departmentClass)lstFinalClasses.SelectedItem;
            txtFinalInfo.Text = "";
            int excludedSections = 0;
            lstExclude.Items.Clear();           //Clears the GUI list of sections for exclusion
            foreach (classSection item in selectedClass.sections)
            {
                if (item.excluded)
                {
                    excludedSections++;
                }
                else
                {
                    lstExclude.Items.Add(item); //add to the excludable list

                }
            }
            if (excludedSections > 0)
            {
                txtFinalInfo.Text += "YOU HAVE EXCLUDED " + excludedSections + " SECTIONS FROM THIS CLASS!\n";
            }
            txtFinalInfo.Text = selectedClass.getClassInfo();
            btnRemoveFromList.IsEnabled = true; //Allows items to be removed from list list
            btnSubmit.IsEnabled = true;
        }

        /* Called when the remove button is clicked.
         * 
         * Removes a class from the final classes list.
         */
        private void btnRemoveFromList_Click(object sender, RoutedEventArgs e)
        {
            departmentClass selectedClass = (departmentClass)lstFinalClasses.SelectedItem;

            lstFinalClasses.Items.Remove(selectedClass);
            txtFinalInfo.Text = "";
            btnRemoveFromList.IsEnabled = false;
            lstExclude.Items.Clear();
            txtExclude.Text = "";
        }

        /* Returns if a number is between (or equal to) two other numbers.
         * @param start : The lower bound
         * @param question : The number in question. The one we're trying to determine if it's between or not.
         * @param end : The higher bound
         * 
         * @return A boolean value. True if the question number is between the other two. False otherwise.
         */

        private bool isBetween(int start, int question, int end)
        {
            if (question >= start && question <= end)
            {
                return true;
            }
            return false;
        }

        private bool isBetween(double start, double question, double end)
        {
            if (question >= start && question <= end)
            {
                return true;
            }
            return false;
        }

        /* Asks if two time ranges given as strings are intersecting.
         *  Time format: "11:30 am - 12:30 pm"
         * We do this by converting the times to a whole number system that can be used to represent different times.
         *  EX: 1:00 = 1, 1:10 = 2, etc. That way we can test for intersection by comparing the whole numbers.
         * 
         * @param time1 : A string that represents the first time gap.
         * @param time2 : A string that represents the second time gap.
         * 
         * @return A boolean value that is true if the times intersect. False otherwise.
         */
        public bool doTimesIntersect(String time1, String time2)
        {
            String startTime, endTime;
            startTime = time1.Split("-".ToCharArray(), StringSplitOptions.None)[0];
            endTime = time1.Split("-".ToCharArray(), StringSplitOptions.None)[1];
            startTime = startTime.Replace(" ", "");
            endTime = endTime.Replace(" ", "");
            string startMeridian = startTime.Substring(startTime.Length - 2, 2);
            string endMeridian = endTime.Substring(endTime.Length - 2, 2);
            startTime = startTime.Substring(0, startTime.Length - 2);
            endTime = endTime.Substring(0, endTime.Length - 2);
            //Get the first part to determine where to start
            string[] splitStartTime = startTime.Split(':');
            string[] splitEndTime = endTime.Split(':');
            int hourStart = Convert.ToInt32(splitStartTime[0]);
            int hourEnd = Convert.ToInt32(splitEndTime[0]);
            int minutesStart = Convert.ToInt32(splitStartTime[1]);
            int minutesEnd = Convert.ToInt32(splitEndTime[1]);
            if (startMeridian == "pm") hourStart += 12;
            if (endMeridian == "pm") hourEnd += 12;
            int startValue = (hourStart * 3) + (minutesStart / 10);
            int endValue = (hourEnd * 3) + (minutesEnd / 10);

            String startTime2, endTime2 = "";
            startTime2 = time2.Split("-".ToCharArray(), StringSplitOptions.None)[0];
            endTime2 = time2.Split("-".ToCharArray(), StringSplitOptions.None)[1];
            startTime2 = startTime2.Replace(" ", "");
            endTime2 = endTime2.Replace(" ", "");
            string startMeridian2 = startTime2.Substring(startTime2.Length - 2, 2);
            string endMeridian2 = endTime2.Substring(endTime2.Length - 2, 2);
            startTime2 = startTime2.Substring(0, startTime2.Length - 2);
            endTime2 = endTime2.Substring(0, endTime2.Length - 2);
            //Get the first part to determine where to start
            string[] splitStartTime2 = startTime2.Split(':');
            string[] splitEndTime2 = endTime2.Split(':');
            int hourStart2 = Convert.ToInt32(splitStartTime2[0]);
            int hourEnd2 = Convert.ToInt32(splitEndTime2[0]);
            int minutesStart2 = Convert.ToInt32(splitStartTime2[1]);
            int minutesEnd2 = Convert.ToInt32(splitEndTime2[1]);
            if (startMeridian2 == "pm") hourStart2 += 12;
            if (endMeridian2 == "pm") hourEnd2 += 12;
            int startValue2 = (hourStart2 * 3) + (minutesStart2 / 10);
            int endValue2 = (hourEnd2 * 3) + (minutesEnd2 / 10);

            //Check if they intersect
            if (isBetween(startValue, startValue2, endValue)
                || isBetween(startValue, endValue2, endValue)
                || isBetween(startValue2, startValue, endValue2)
                || isBetween(startValue2, endValue, endValue2))
            {
                return true;
            }
            return false;
        }

        /* Called in a background thread. Used to generate bitstrings of a specified length. These strings
         *  are represented as an array of bools - true = 1, false = 0. This method is more efficient.
         *  EX: ("11001")
         * 
         * We use these bitstrings to represent all possible combinations of the sections (even invalid ones).
         * 
         * @param e.Argument This value passed through the BackgroundWorker call is the length of the bitstring.
         */
        private void generateAllBitStrings(object sender, DoWorkEventArgs e)
        {
            short n = Convert.ToInt16(e.Argument as string);          //The length of the bitstrings

            /* Progress reporting */
            double nsquared = Math.Pow(2,n);                          //This is the target number of strings to generate
            //TODO cleanup
            double percentStep = nsquared / 100;
            if (percentStep == 0) percentStep = 1;
            double percentJump = nsquared / percentStep;
            if (percentJump == 0) percentJump = 1;
            else percentJump = 100 / percentJump;
            //Start timekeeping
            //MyStopwatch timer = new MyStopwatch();
            
            double totalResults = 0.0;            //Total results for progress reporting
            int multiplier = 2;                   //Increases the time between timeleft updates (exponential)
            double rollingTotal = 0.0;            //Total results generated that resets after timeleft update

            /* Begin generating bitstrings */
            bool[] first = new bool[n];       //First string is all false
            bool[] target = new bool[n];      //End string is all true
            for (short i = 0; i < n; i++)
            {
                first[i] = false;
                target[i] = true;
            }
            bool[] intermediate = first;

            checkBitstring(first);
            totalResults++;

            /* Generating loop */
            MyStopwatch timer = new MyStopwatch();
            do
            {
                if (rollingTotal == 0)
                {
                    timer.start();
                }
                //Make the next bitstring
                intermediate = generateNextBitstring(intermediate);
                
                //Increment counters
                totalResults++;
                rollingTotal++;

                //Check the bitstring we generated
                checkBitstring(intermediate);

                //Update the time left in exponential time
                if (rollingTotal == Math.Pow(2, multiplier))
                {
                    float averageTime = (float)timer.stop() / (float)Math.Pow(2, multiplier);
                    secondsRemaining = (int)((averageTime * (nsquared - totalResults)) / 1000.0) + 2;
                    multiplier++;
                    rollingTotal = 0;
                }

                //Update the progress bar and report progress to update the GUI with valid combinations
                if (isBetween(0.0, totalResults % percentStep, 0.99))
                {
                    this.Dispatcher.BeginInvoke(delegate { progress.Value += percentJump; });
                    (sender as BackgroundWorker).ReportProgress(0, null);
                }

                //If we're performing too quickly, we need to slow down for the other threads to catch up.
                if ((nsquared < 10000) && isBetween(0, totalResults % 2, 0.99))
                {
                    Thread.Sleep(5);
                }
            } while (totalResults < nsquared);
            timer.stop();
            Debug.WriteLine("Process took "+ (float)(timer.interval / 1000f) + " seconds.");
            /* After generating loop */

            //Because the math for the percent step/jump isn't 100% accurate, when we are done checking combinations
            // we should "cheat" and set the progress bar to 100.
            this.Dispatcher.BeginInvoke(delegate { progress.Value = 100; });
            (sender as BackgroundWorker).ReportProgress(100, null);

            //Cancel the time reporting functions
            counterDown.CancelAsync();
            secondsRemaining = 0;
            this.Dispatcher.BeginInvoke(updateTimeLabel);
        }

        /* Generates the next bitstring.
         * 
         * @param old The previous bitstring
         * 
         * @return The old array modified as a new bitstring
         */
        private bool[] generateNextBitstring(bool[] old)
        {
            //Get length of the string
            short lastIndex = (short)(old.Length - 1);
            short targetIndex = 0;
            //Check where the first false occurs from the right side of the "string"
            //Flip that false to true
            for (short n = lastIndex; n >= 0; n--)
            {
                if (old[n] == false)
                {
                    targetIndex = (short)(n + 1);
                    old[n] = true;
                    break;
                }
            }
            //From that first false to the end of the string, mark falses
            for (short n = targetIndex; n <= lastIndex; n++)
            {
                old[n] = false;
            }
            return old;
        }

        /* Evaluates bitstrings as section combinations.
         *  If a combination is a valid schedule, then we add it to a global list of confirmed classes.
         * 
         * @param bitstring The bool array that represents a bitstring
         */
        private void checkBitstring(bool[] bitstring)
        {
            List<Course> possibility = new List<Course>();
            int iterator = 0;
            foreach (bool bit in bitstring)
            {
                if (bit == true)
                {
                    possibility.Add(possibleSections[iterator]);
                }
                iterator++;
            }
            //If the combination works, add it to the list of possibilities
            if (combinationSuccessful(possibility))
            {
                lock (confirmedSections)
                {
                    confirmedSections.Add(possibility);
                }
            }
        }

        /* Tests if a collection of courses are valid for a schedule.
         * 
         * @param possibilites A list of courses that need evaluated.
         * 
         * @return A boolean value - true if the combination is valid, false otherwise.
         */
        private bool combinationSuccessful(List<Course> possibilities)
        {
            //First check that all classes are represented.
            int classCount = totalClassCount;
            Dictionary<string, List<Course>> classesDict = new Dictionary<string, List<Course>>();  //"class", List of sections
            foreach (Course current in possibilities)
            {
                string parent = current.parentClass;
                if (!(classesDict.ContainsKey(parent)))
                {
                    //If the class name isn't in the dictionary, add it.
                    List<Course> toAdd = new List<Course>();
                    toAdd.Add(current);
                    classesDict.Add(parent, toAdd);
                }
                else
                {
                    //Add the current course under the parent class name in the dictionary.
                    List<Course> courses;
                    classesDict.TryGetValue(current.parentClass, out courses);
                    courses.Add(current);
                }
            }
            if (classesDict.Keys.Count != classCount)
            {
                //If the number of keys are not equal to the number of classes
                return false;
            }
            else
            {
                //Now let's rule out if there are two sections with the same linkID.
                foreach (string key in classesDict.Keys)
                {
                    List<Course> classSections = new List<Course>();
                    List<string> links = new List<string>();
                    List<string> linksTo = new List<string>();
                    classesDict.TryGetValue(key, out classSections);
                    foreach (Course linkTestCourse in classSections)
                    {
                        if (!links.Contains(linkTestCourse.linkID))
                        {
                            //If our link doesn't exist in the list yet, add it.
                            links.Add(linkTestCourse.linkID);
                        }
                        //If it's already in the list, the combination is not allowed.
                        else return false;

                        //Check that each class is linked uniquely. IE: A1 and A2 link to A3. Only A1 OR A2 are allowed.
                        if (linkTestCourse.linked)
                        {
                            if (!linksTo.Contains(linkTestCourse.linkedToID))
                            {
                                linksTo.Add(linkTestCourse.linkedToID);
                            }
                            else return false;
                        }
                    }
                    //Also, we know that there are courses with the same linkID, let's make sure their link exists.
                    foreach (Course linkedToTestCourse in classSections)
                    {
                        //If the course isn't linked, then there is no point in looking to make sure it's not linked to something
                        if (!linkedToTestCourse.linked) continue;
                        //If the linkID this course is linked to isn't present in the list, then this combination sucks.
                        if (!links.Contains(linkedToTestCourse.linkedToID))
                        {
                            return false;
                        }
                    }
                }
                //Check for intersections
                //Put each day's meetings into a dictionary organized by day.
                Dictionary<char, List<Course>> dayMeetingsDict = new Dictionary<char, List<Course>>();
                foreach (Course timeIntersection in possibilities)
                {
                    if (timeIntersection.days.ToLower().Equals("tba")
                        || timeIntersection.days.ToLower().Equals(" ")) continue; //We don't care about TBA courses
                    foreach (char day in timeIntersection.days)
                    {
                        //If the day isn't in the dictionary already, add it
                        if (!dayMeetingsDict.Keys.Contains(day))
                        {
                            List<Course> tempList = new List<Course>();
                            tempList.Add(timeIntersection);
                            dayMeetingsDict.Add(day, tempList);
                        }
                        else
                        {
                            List<Course> currentList = new List<Course>();
                            dayMeetingsDict.TryGetValue(day, out currentList);
                            currentList.Add(timeIntersection);
                        }
                    }
                }
                //Now iterate through the dictionary day by day and check for intersections within.
                foreach (var kv in dayMeetingsDict)
                {
                    List<Course> meetingsThisDay = kv.Value;
                    if (meetingsThisDay.Count == 1) continue; //If there's only 1 meeting that day, no need to check.
                    foreach (Course course in meetingsThisDay)
                    {
                        foreach (Course course2 in meetingsThisDay)
                        {
                            if (course2 == course) continue;
                            string course1Time = course.startTime + " - " + course.endTime;
                            string course2Time = course2.startTime + " - " + course2.endTime;
                            if (doTimesIntersect(course1Time, course2Time))
                            {
                                return false;
                            }
                        }
                    }
                }

                //If we never return in those loops, then it's still valid.
                return true;
            }
        }

        /* Called when the bitstring worker reports progress.
         *  Should update the GUI to reflect schedules that work
         */
        private void reportProgress(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 100)
            {
                btnSubmit.IsEnabled = true;    //Reenable the submit button now that we're (mostly) done.
                //Thread.Sleep(500);             //Give the checking worker some time to finish.
            }
            List<List<Course>> reportingList = new List<List<Course>>();
            lock (confirmedSections)
            {
                foreach (List<Course> classGroup in confirmedSections)
                {
                    reportingList.Add(classGroup);
                }
                //Clear the list since we used them all.
                confirmedSections = new List<List<Course>>();
            }
            if ( reportingList.Count < 1 ) return;  //If there are no elements, draw nothing.
            foreach (List<Course> classGroup in reportingList)
            {
                //Make a grid and tab
                tabCount++;
                TabItem tab = new TabItem();
                tab.Header = ""+tabCount;          //The tab name
                Grid grid = new Grid();
                //Draw horizontal lines
                for (int i = 0; i < 80; i++)
                {
                    Line gridLines = new Line();
                    gridLines.X1 = 40;
                    gridLines.X2 = tabBase.ActualWidth - 89;
                    gridLines.Y1 = 60 + 6.7*i;
                    gridLines.Y2 = 60 + 6.7*i;
                    if (i % 3 == 0) gridLines.Stroke = new SolidColorBrush(Colors.DarkGray);
                    else gridLines.Stroke = new SolidColorBrush(Colors.Transparent);
                    gridLines.StrokeThickness = 0.5f;
                    grid.Children.Add(gridLines);
                }
                //Draw vertical lines
                for (int i = 0; i < 6; i++)
                {
                    Line gridLines = new Line();
                    gridLines.X1 = 40 + ((tabBase.ActualWidth / 6) + .80f) * i;
                    gridLines.X2 = 40 + ((tabBase.ActualWidth / 6) + .80f) * i;
                    gridLines.Y1 = 60;
                    gridLines.Y2 = 583;
                    gridLines.Stroke = new SolidColorBrush(Colors.DarkGray);
                    gridLines.StrokeThickness = 0.5f;
                    grid.Children.Add(gridLines);
                }

                //Draw each course on the grid
                tbaCount = 0;
                List<Course> coursesInGroup = new List<Course>();
                foreach (Course course in classGroup)
                {
                    drawSchedule(grid, course.startTime, course.endTime, course.days, course.parentClass + "\n" + course.type);
                    coursesInGroup.Add(course);
                }

                tab.Content = grid;
                tabBase.Items.Add(tab);
                sectionsInGrid.Add(grid, coursesInGroup);
            } 
        }

        /* Called when the submit button is clicked. Initiliazes some data and starts the schedule
         *  generation process.
         */
        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            //Clear all of the generated tabs
            tabBase.SelectedIndex = 0;
            for (int tabIter = (tabBase.Items.Count-1); tabIter > 1; tabIter--)
            {
                tabBase.Items.RemoveAt(tabIter);
            }
            totalClassCount = lstFinalClasses.Items.Count;
            possibleSections = new List<Course>();              //All of the possible courses
            progress.Value = 0;                                 //Reset the progress bar
            tabCount = 0;                                       //Reset the tab numberings.
            secondsRemaining = 0;                               //Reset timeleft
            sectionsInGrid = new Dictionary<Grid, List<Course>>();  //Reset the grid dictionary
            //Start finding bitstrings in the background
            BackgroundWorker bitstringworker = new BackgroundWorker();
            bitstringworker.WorkerSupportsCancellation = false;
            bitstringworker.WorkerReportsProgress = true;
            bitstringworker.DoWork += new DoWorkEventHandler(generateAllBitStrings);
            bitstringworker.ProgressChanged += new ProgressChangedEventHandler(reportProgress);
            //Count how many courses there are so we know how many bitstrings to generate
            int totalCourses = 0;
            foreach (Class finalClass in lstFinalClasses.Items)
            {
                foreach (Course course in finalClass.sections)
                {
                    //If course is not in exlusions
                    if (!course.excluded)
                    {
                        possibleSections.Add(course);
                        totalCourses++;
                    }
                }
            }
            //Start counting down.
            counterDown = new BackgroundWorker();
            counterDown.WorkerSupportsCancellation = true;
            counterDown.WorkerReportsProgress = false;
            counterDown.DoWork += new DoWorkEventHandler(decreaseTimeRemaining);
            counterDown.RunWorkerAsync();
            //Start generating bitstrings in background
            bitstringworker.RunWorkerAsync("" + totalCourses);
            //Disable the submit button while we're generating. 
            btnSubmit.IsEnabled = false;
        }

        /* Called while the bitstring worker is running
         *  Updates the estimated time left.
         */
        private void decreaseTimeRemaining(object sender, DoWorkEventArgs e)
        {
            if (counterDown.CancellationPending) return;
            if (secondsRemaining == 0) Thread.Sleep(1000);
            while (secondsRemaining > 0)
            {
                if (counterDown.CancellationPending) return;
                secondsRemaining--;
                this.Dispatcher.BeginInvoke(updateTimeLabel);
                Thread.Sleep(1000);
            }
        }

        /*
         * Updates the estimated time remaining label
         */
        private void updateTimeLabel()
        {
            if (secondsRemaining == 1)
            {
                lblTimeRemaining.Content = "Estimated time remaining: " + secondsRemaining + " second";
                return;
            }
            lblTimeRemaining.Content = "Estimated time remaining: " + secondsRemaining + " seconds";
        }

        /* Draws a section's meeting times on the specified grid.
         * 
         * @param grid : The grid to draw on
         * @param startTime : The starting time of the section  (Form: "11:30 am")
         * @param endTime : The ending time of the section      (Form: "12:30 pm")
         * @param days : The days the section meets
         */
        private void drawSchedule(Grid grid, string startTime, string endTime, string days, string labelText = "")
        {
            //If the time is TBA, it needs to be drawn at the bottom.
            bool tba = false;
            if (startTime.ToLower().Equals("tba"))
            {
                tba = true;
            }
            string startMeridian, endMeridian;
            int hourStart, hourEnd;
            string[] splitStartTime, splitEndTime;
            if (!tba)
            {
                //Change "11:30 am" to "11:30"
                startTime = startTime.Replace(" ", "");
                endTime = endTime.Replace(" ", "");
                //Begin parsing the time string for what we need. "11", "30", "am"
                startMeridian = startTime.Substring(startTime.Length - 2, 2);
                endMeridian = endTime.Substring(endTime.Length - 2, 2);
                startTime = startTime.Substring(0, startTime.Length - 2);
                endTime = endTime.Substring(0, endTime.Length - 2);
                //Get the first part to determine where to start
                splitStartTime = startTime.Split(':');
                splitEndTime = endTime.Split(':');
                hourStart = Convert.ToInt32(splitStartTime[0]);
                hourEnd = Convert.ToInt32(splitEndTime[0]);
                days = days.ToLower();
            }
            else
            {
                startMeridian = "pm";
                endMeridian = "pm";
                hourStart = 9 + (tbaCount % 2);
                hourEnd = 10 + (tbaCount % 2);
                splitStartTime = "0:0".Split(':');
                splitEndTime = "0:0".Split(':');

                //We can't have too many TBA courses on one day or it would draw off screen.
                switch (tbaCount)
                {
                    case 0:
                    case 1:
                        days = "m";
                        break;
                    case 2:
                    case 3:
                        days = "t";
                        break;
                    case 4:
                    case 5:
                        days = "w";
                        break;
                    case 6:
                    case 7:
                        days = "r";
                        break;
                    case 8:
                    case 9:
                        days = "f";
                        break;
                    default:
                        //This should be impossible. 8 distance learning classes?  As if.
                        days = "f";
                        break;
                }
                tbaCount++;
            }
            //Do position calculation for the start time.
            // Our system starts at 7:00 am, but the grid starts at 7:30. So we start the position at 3.
            // Then, every 10 minutes is another position, so each hour is 6 positions.
            int positionStart = 3;
            //Make our clock a 24 hour system
            if (startMeridian == "pm" && hourStart != 12) hourStart += 12;
            //The hour of the first class is at 7 -- so 
            hourStart -= 7;
            positionStart += 6 * (hourStart - 1);

            positionStart += (int)((Math.Floor((Convert.ToDouble(splitStartTime[1])))) / 10.0);

            //Position calculation for the end time. Same as above.
            int positionEnd = 3;
            //Make our clock a 24 hour system
            if (endMeridian == "pm" && hourEnd != 12) hourEnd += 12;
            //The hour of the first class is at 7
            hourEnd -= 7;
            positionEnd += 6 * (hourEnd - 1);

            positionEnd += (int)((Math.Floor((Convert.ToDouble(splitEndTime[1])))) / 10.0);

            Canvas canvas = new Canvas();
            foreach (char character in days)
            {
                Rectangle block = new Rectangle();
                canvas.Children.Add(block);
                block.Fill = new SolidColorBrush(Colors.Cyan);
                block.Stroke = new SolidColorBrush(Colors.Black);
                block.StrokeThickness = 1.2d;
                block.RadiusX = block.RadiusY = 5.0d;               //Rounds the block corner.
                block.Width = ((tabBase.ActualWidth / 6) + .80f);
                block.Height = (positionEnd - positionStart) * 6.7; //Each position is 6.7 points tall.
                Label info = new Label();
                if (labelText.ToLower().IndexOf("observation") > 0)
                {
                    labelText = labelText.Replace("Practice Study Observation", "PSO");
                }
                info.Content = labelText;
                canvas.Children.Add(info);
                switch (character)
                {
                    case 'm':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d);
                        info.SetValue(Canvas.TopProperty, 61d + (Double)(6.7 * positionStart));
                        info.SetValue(Canvas.LeftProperty, 45d);
                        break;
                    case 't':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d + ((tabBase.ActualWidth / 6) + .80f) * 1);
                        info.SetValue(Canvas.TopProperty, 61d + (Double)(6.7 * positionStart));
                        info.SetValue(Canvas.LeftProperty, 45d + ((tabBase.ActualWidth / 6) + .80f) * 1);
                        break;
                    case 'w':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d + ((tabBase.ActualWidth / 6) + .80f) * 2);
                        info.SetValue(Canvas.TopProperty, 61d + (Double)(6.7 * positionStart));
                        info.SetValue(Canvas.LeftProperty, 45d + ((tabBase.ActualWidth / 6) + .80f) * 2);
                        break;
                    case 'r':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d + ((tabBase.ActualWidth / 6) + .80f) * 3);
                        info.SetValue(Canvas.TopProperty, 61d + (Double)(6.7 * positionStart));
                        info.SetValue(Canvas.LeftProperty, 45d + ((tabBase.ActualWidth / 6) + .80f) * 3);
                        break;
                    case 'f':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d + ((tabBase.ActualWidth / 6) + .80f) * 4);
                        info.SetValue(Canvas.TopProperty, 61d + (Double)(6.7 * positionStart));
                        info.SetValue(Canvas.LeftProperty, 45d + ((tabBase.ActualWidth / 6) + .80f) * 4);
                        break;
                }
            }
            grid.Children.Add(canvas);
        }

        /* Called when the tab is switched.
         * 
         * Because I'm using a hidden "template" tab for placing the times and day names on a tab,
         *  I have to make sure the tab is only being used in one tab at a time. So when the tab
         *  selection changes, I remove the template tab from removed tab, and add it to the tab
         *  that is about to be displayed.
         */
        private void tabBase_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count < 1) return;
            if (e.RemovedItems[0] == mainTab)
            {
            }
            else if (e.AddedItems[0] == mainTab)
            {
                //If we're switching to the main tab, we want to make sure that we don't overlay times and days
                // like the other tabs.
                ((Grid)((TabItem)e.RemovedItems[0]).Content).Children.Remove(grdSchedule);
                return;
            }
            ((Grid)((TabItem)e.RemovedItems[0]).Content).Children.Remove(grdSchedule);
            ((Grid)((TabItem)e.AddedItems[0]).Content).Children.Add(grdSchedule);
        }

        /* Called when the exclusion checkbox is checked.
         * 
         * We just update the visibility.
         */
        private void chkExclude_Checked(object sender, RoutedEventArgs e)
        {
            lstExclude.Visibility = System.Windows.Visibility.Visible;
            txtExclude.Visibility = System.Windows.Visibility.Visible;
            btnExclude.Visibility = System.Windows.Visibility.Visible;
            lblExcludeInfo.Visibility = System.Windows.Visibility.Visible;
        }

        /* Called when the exclusion checkbox is checked.
         * 
         * We just update the visibility.
         */
        private void chkExclude_Unchecked(object sender, RoutedEventArgs e)
        {
            //Not visible
            lstExclude.Visibility = System.Windows.Visibility.Collapsed;
            txtExclude.Visibility = System.Windows.Visibility.Collapsed;
            btnExclude.Visibility = System.Windows.Visibility.Collapsed;
            lblExcludeInfo.Visibility = System.Windows.Visibility.Collapsed;
        }

        /* Called when the selection changes in the exclusion list.
         * 
         * We want to display information just like in the class info box. 
         */
        private void lstExclude_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0 && e.AddedItems.Count < 1)
            {
                return;
            }
            classSection selectedSection = (classSection)lstExclude.SelectedItem;
            txtExclude.Text = selectedSection.getSectionInfo();
        }

        /* Called when the tab removal button is clicked.
         * 
         * We're going to move the tab that's selected in a safe direction and then remove the tab.
         */
        private void btnRemoveTab_Click_1(object sender, RoutedEventArgs e)
        {
            int currentTabIndex = tabBase.SelectedIndex;
            //Need to make sure that we don't accidently switch to our hidden template tab.
            if ((currentTabIndex - 1) == 1)
            {
                tabBase.SelectedIndex = 0;
            }
            else
            {
                tabBase.SelectedIndex = (currentTabIndex - 1);
            }
            tabBase.Items.RemoveAt(currentTabIndex);
        }

        /* Called when the exclude button is clicked.
         * 
         *  Removes or adds sections from consideration.
         */
        private void btnExclude_Click_1(object sender, RoutedEventArgs e)
        {
            classSection selectedSection = (classSection)lstExclude.SelectedItem;
            bool currentlyExcluded = selectedSection.excluded;
            if (currentlyExcluded)
            {
                selectedSection.excluded = false;
            }
            else
            {
                selectedSection.excluded = true;
            }
            //Update the text for the list
            lstExclude.Items.Remove(selectedSection);
            lstExclude.Items.Add(selectedSection);
            lstExclude.SelectedItem = selectedSection;
        }

        private void btnChoose_Click(object sender, RoutedEventArgs e)
        {
            Popup popper = new Popup();
            Grid parent = (Grid)((TabItem)(tabBase.SelectedItem)).Content;
            List<Course> sections = new List<Course>();
            sectionsInGrid.TryGetValue(parent, out sections);

            Border border = new Border();
            border.BorderBrush = new SolidColorBrush(Colors.Black);
            border.BorderThickness = new Thickness(2.0);

            StackPanel panel1 = new StackPanel();
            panel1.Background = new SolidColorBrush(Colors.LightGray);

            Button button1 = new Button();
            button1.Content = "Close";
            button1.Margin = new Thickness(5.0);
            button1.Click += new RoutedEventHandler(buttonClosePopup_click);

            TextBlock textblock1 = new TextBlock();
            textblock1.Text = "";
            foreach (Course result in sections)
            {
                textblock1.Text += "Course: " + result.parentClass + " " + result.type + " | CRN: " + result.crn + "\n";
            }
            textblock1.Margin = new Thickness(5.0);
            panel1.Children.Add(textblock1);
            panel1.Children.Add(button1);
            border.Child = panel1;

            // Set the Child property of Popup to the border 
            // which contains a stackpanel, textblock and button.
            popper.Child = border;

            // Set where the popup will show up on the screen.
            popper.VerticalOffset = 250;
            popper.HorizontalOffset = 250;
            popper.IsOpen = true;
        }

        /* Called when the close button on a popup is clicked
         * 
         * Closes the popup.
         */
        private void buttonClosePopup_click(object sender, RoutedEventArgs e)
        {
            //HA! is all I have to say to this.
            ((Popup)((Border)(((StackPanel)((Button)sender).Parent).Parent)).Parent).IsOpen = false;
        }


    }
}
