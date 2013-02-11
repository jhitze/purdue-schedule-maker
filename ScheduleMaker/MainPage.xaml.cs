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

//For debug
using System.Diagnostics;

namespace ScheduleMaker
{
    public partial class MainPage : UserControl
    {
        List<Class> classes;                    //A list of all the classes
        private int classesIndex = 0;           //An index that represents the next free space in the classes list.
        List<Course> possibleSections;          //A list of sections that are about to be added to the scheduling
        int totalClassCount = 0;                //The total number of classes
        List<List<Course>> confirmedSections;   //A list of lists of courses grouped by schedule grid
        int tabCount = 0;                       //A number used to label the tabs.
        int tbaCount = 0;                       //The number of classes that have times of TBA. We need a count to track position.

        /* Constructor.
         * 
         * Here we start all the GUI stuff.
         */
        public MainPage()
        {
            InitializeComponent();
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
                int position = -1;                   //The position of where the letters end and numbers start
                input = input.Replace(" ", "");      //Make it fit the format we're expecting -- without spaces
                //Check each character in the input string.
                foreach (char thing in input)
                {
                    position++;
                    //Attempt to convert each character to a number. If it fails, it's a letter.
                    try
                    {
                        Convert.ToInt32(thing.ToString());
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
                if (position == 1) position++;  //We can't take a substring of length one. So cheat and call it 2.
                string department = input.Substring(0, position).ToLower();
                string number;                  //This is the course number ie "18000"
                if (department.Length + 1 == input.Length) number = ""; 
                else number = input.Substring(position);
                //Perform search
                XmlReader reader = XmlReader.Create("classdata.xml");
                do
                {
                    //Navigate to the correct department
                    if (!reader.ReadToFollowing("department"))
                    {
                        break;
                    }
                    
                } while (reader.GetAttribute(0).ToLower() != department);

                bool block = false; //If we're in a class element that we're not looking for, we need to block
                                    //reading the courses in that element.

                //Read in all the classes in the department
                do
                {
                    reader.Read();  //Read the next node
                    switch(reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name)
                            {
                                case "class":
                                    //The node is a class element and we need to add it to the class list
                                    //  if it's one we're looking for.
                                    if (number == "" || reader.GetAttribute(0).ToLower().IndexOf(number) >= 0)
                                    {
                                        classes.Add(new Class());
                                        classes[classesIndex] = new Class();
                                        classes[classesIndex].name = reader.GetAttribute(3);
                                        classes[classesIndex].description = reader.GetAttribute(2);
                                        classes[classesIndex].credits = reader.GetAttribute(1);
                                        classes[classesIndex].course = reader.GetAttribute(0);
                                    }
                                    else block = true;
                                    break;
                                case "section":
                                    //If we're in a correct class element, we're going to get the data for the course.
                                    if (!block)
                                    {
                                        Course course = new Course();

                                        course.parentClass = classes[classesIndex].ToString();
                                        course.availability = reader.GetAttribute("availability");
                                        course.crn = reader.GetAttribute("crn");
                                        course.days = reader.GetAttribute("days");
                                        course.instructor = reader.GetAttribute("instructor");
                                        course.type = reader.GetAttribute("type");
                                        bool linked = reader.GetAttribute("linkdid") != "0";
                                        if (linked)
                                        {
                                            course.linked = linked;
                                            course.linkID = reader.GetAttribute("linkid");
                                            course.linkedToID = reader.GetAttribute("linkedtoid");
                                        }
                                        if (reader.GetAttribute("time") != "TBA")
                                        {
                                            string[] time = reader.GetAttribute("time").Split('-');
                                            course.startTime = time[0].Trim();
                                            course.endTime = time[1].Trim();
                                        }
                                        else
                                        {
                                            course.startTime = "TBA";
                                            course.endTime = "TBA";
                                        }
                                        classes[classesIndex].sections.Add(course);
                                    }
                                    break;
                            }
                            break;
                        default:
                            break;
                    }
                    //If the element is the end part (</element>), we need to do some cleaning up.
                    if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (reader.Name == "department") break; //If we leave the department we're looking in, return.
                        if (reader.Name == "class")
                        {
                            if (block)
                            {
                                block = false;
                            }
                            else classesIndex++;
                        }
                    }
                } while (!reader.EOF);
                // All the class data is loaded
                foreach (Class entry in classes)
                {
                    //Add all of the classes we found to the GUI class list
                    classList.Items.Add(entry);
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
            Class classToDisplay = (Class)e.AddedItems[0];
            classInfoBox.Text = "";
            classInfoBox.Text += classToDisplay.course + " - " + classToDisplay.name + '\n' + '\n';
            classInfoBox.Text += classToDisplay.description + "\n";
            classInfoBox.Text += classToDisplay.credits;
            btnAddToList.IsEnabled = true;  //If there is an item selected, we can allow it to be added to the pending classes
        }

        /* This function is called when the add button is clicked.
         *
         * Moves the currently selected item from the searched classes to the pending classes.
         */
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Class classToMove = (Class)classList.SelectedItem;
            lstFinalClasses.Items.Add(classToMove);
            classList.Items.Remove(classToMove);
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

            Class classToDisplay = (Class)e.AddedItems[0];
            txtFinalInfo.Text = "";
            txtFinalInfo.Text += classToDisplay.course + " - " + classToDisplay.name + '\n' + '\n';
            txtFinalInfo.Text += classToDisplay.description + "\n";
            txtFinalInfo.Text += classToDisplay.credits;
            btnRemoveFromList.IsEnabled = true; //Allows items to be removed from list list
            btnSubmit.IsEnabled = true;
            
            lstExclude.Items.Clear();           //Clears the GUI list of sections for exclusion
            //Add each section for the selected course to the excludable items list.
            foreach (Course course in classToDisplay.sections)
            {
                lstExclude.Items.Add(course);
            }
        }

        /* Called when the remove button is clicked.
         * 
         * Removes a class from the final classes list.
         */
        private void btnRemoveFromList_Click(object sender, RoutedEventArgs e)
        {
            Class classToRemove = (Class)lstFinalClasses.SelectedItem;

            lstFinalClasses.Items.Remove(classToRemove);
            txtFinalInfo.Text = "";
            btnRemoveFromList.IsEnabled = false;
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
        private bool doTimesIntersect(String time1, String time2)
        {
            String startTime, endTime = "";
            startTime = time1.Split(" - ".ToCharArray(), StringSplitOptions.None)[0];
            endTime = time1.Split(" - ".ToCharArray(), StringSplitOptions.None)[1];
            startTime.Replace(" ", "");
            endTime.Replace(" ", "");
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
            if (endMeridian == "pm") hourEnd += 12;
            int startValue = (hourStart * 3) + (minutesStart / 10);
            int endValue = (hourEnd * 3) + (minutesEnd / 10);

            String startTime2, endTime2 = "";
            startTime2 = time2.Split(" - ".ToCharArray(), StringSplitOptions.None)[0];
            endTime2 = time2.Split(" - ".ToCharArray(), StringSplitOptions.None)[1];
            startTime2.Replace(" ", "");
            endTime2.Replace(" ", "");
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

        /* Called in a background thread. Used to generate bitstrings of a specified length. EX: ("11001")
         *  Actually runs pretty quickly, until you get to about length 23 and above. 2^23 strings of length 23
         *  isn't a fast process.
         * 
         * We use these bitstrings to represent all possible combinations of the sections (even invalid ones).
         * 
         * @param e.Argument This value passed through the BackgroundWorker call is the length of the bitstring.
         */
        private void generateAllBitStrings(object sender, DoWorkEventArgs e)
        {
            int n = Convert.ToInt32(e.Argument as string);          //The length of the bitstrings
            List<string> results = new List<string>();              //A collection of the bitstrings so far
            int[] cutoffs = new int[n + 1];
            Array.Clear(cutoffs, 0, n+1);                           //Initialize the array to 0s
            int nsquared = (int) Math.Pow(2,n);                     //This is the target number of strings to generate
            //Stuff below here is for calculating how to update the progress bar
            int percentStep = nsquared / 100;
            if (percentStep == 0) percentStep = 1;
            int percentJump = nsquared / percentStep;
            if (percentJump == 0) percentJump = 1;
            else percentJump = 100 / percentJump;
            //Begin bitstring generation
            for (int row = 1; row <= nsquared; row++)
            {
                string bitstring = "";
                for (int column = 1; column <= n; column++)
                {
                    int cutoff = nsquared / (int)Math.Pow(2, column);
                    if (cutoffs[column] == 0)
                    {
                        cutoffs[column] = cutoff;
                    }
                    if ((row % (cutoff * 2)) == 1)
                    {
                        cutoffs[column] = row+(cutoff-1);
                    }
                    bitstring += (row <= cutoffs[column]) ? "1" : "0";
                }
                results.Add(bitstring);
                //After a certain amount of rows, we need to pass the data into a new thread that will evaluate
                //if our bitstring candidates are valid combinations of courses.
                if (row % 10000 == 0 || row == nsquared)
                {
                    //Make a new BackgroundWorker that will test our combinations
                    BackgroundWorker checkstringsWorker = new BackgroundWorker();
                    checkstringsWorker.WorkerSupportsCancellation = false;
                    checkstringsWorker.WorkerReportsProgress = false;
                    checkstringsWorker.DoWork += new DoWorkEventHandler(checkBitstrings);
                    checkstringsWorker.RunWorkerAsync(results);
                    results = new List<string>();
                }
                if (row % percentStep == 0)
                {
                    //Update the progress bar and report a step, which should display combination on the GUI
                    this.Dispatcher.BeginInvoke(delegate { progress.Value += percentJump; });
                    (sender as BackgroundWorker).ReportProgress(0, null);
                }
            }
            //Because the math for the percent step/jump isn't 100% accurate, when we are done checking combinations
            // we should "cheat" and set the progress bar to 100.
            this.Dispatcher.BeginInvoke(delegate { progress.Value = 100; });
            (sender as BackgroundWorker).ReportProgress(100, null);
        }

        /* Evaluates bitstrings as section combinations.
         *  If a combination is a valid schedule, then we add it to a global list of confirmed classes.
         * 
         * @param e.Argument Passed from the caller, it is the list of bitstrings that have been generated
         */
        private void checkBitstrings(object sender, DoWorkEventArgs e)
        {
            foreach (string bitstring in (e.Argument as List<string>))
            {
                List<Course> possibility = new List<Course>();
                int iterator = 0;
                foreach (char bit in bitstring)
                {
                    if (bit == '1')
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
                //If we never return in those loops, then it's still valid.
                return true;
            }
        }

        /* Called when the bitstring worker reports progress.
         *  Should update the GUI to reflect schedules that work
         */
        private void reportProgress(object sender, ProgressChangedEventArgs e)
        {
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
                foreach (Course course in classGroup)
                {
                    drawSchedule(grid, course.startTime, course.endTime, course.days);
                }

                tab.Content = grid;
                tabBase.Items.Add(tab);
            }
            if (e.ProgressPercentage == 100) btnSubmit.IsEnabled = true;    //Reenable the submit button now that we're done.
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
            possibleSections = new List<Course>();               //All of the possible courses
            progress.Value = 0;                                 //Reset the progress bar
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
                    //TODO: if course is not in exlusions:
                    possibleSections.Add(course);
                }
                totalCourses += finalClass.sections.Count;
            }
            //Start generating bitstrings in background
            bitstringworker.RunWorkerAsync("" + totalCourses);
            //Disable the submit button while we're generating. 
            btnSubmit.IsEnabled = false;
        }

        /* Draws a section's meeting times on the specified grid.
         * 
         * @param grid : The grid to draw on
         * @param startTime : The starting time of the section  (Form: "11:30 am")
         * @param endTime : The ending time of the section      (Form: "12:30 pm")
         * @param days : The days the section meets
         */
        private void drawSchedule(Grid grid, string startTime, string endTime, string days)
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
                splitStartTime = "0:30".Split(':');
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
                block.Fill = new SolidColorBrush(Colors.Blue);
                block.Stroke = new SolidColorBrush(Colors.Black);
                block.StrokeThickness = 1.3d;
                block.RadiusX = block.RadiusY = 5.0d;               //Rounds the block corner.
                block.Width = ((tabBase.ActualWidth / 6) + .80f);
                block.Height = (positionEnd - positionStart) * 6.7; //Each position is 6.7 points tall.
                switch (character)
                {
                    case 'm':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d);
                        break;
                    case 't':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d + ((tabBase.ActualWidth / 6) + .80f) * 1);
                        break;
                    case 'w':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d + ((tabBase.ActualWidth / 6) + .80f) * 2);
                        break;
                    case 'r':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d + ((tabBase.ActualWidth / 6) + .80f) * 3);
                        break;
                    case 'f':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d + ((tabBase.ActualWidth / 6) + .80f) * 4);
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
            Course course = (Course)e.AddedItems[0];
            txtExclude.Text = "";
            txtExclude.Text += "CRN: " + course.crn + "\n";
            txtExclude.Text += "Meets on " + course.days + " at " + course.startTime + " - " + course.endTime + "\n";
            txtExclude.Text += "Type: " + course.type + "\n";
            txtExclude.Text += "Instructor: " + course.instructor + "\n";
            txtExclude.Text += "\nAvailability: " + course.availability;
        }
    }
}
