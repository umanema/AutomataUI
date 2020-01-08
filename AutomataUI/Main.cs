#region usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Automata.Data;
using Automata.Drawing;
using Automata.Enumerations;

using VL.Lib.Collections;
#endregion usings


namespace VVVV.Nodes
{
    public class AutomataUI : UserControl, IPartImportsSatisfiedNotification
    {
        #region fields & pins

        public List<bool> FAllowMultiple;

        public int FSpreadCount = 0;
        int prevSpreadCount = 0;

        public List<bool> FocusWindow;

        public string StateXML = "";

        public string TransitionXML = "";

        public string RegionXML = "";

        //public List<string> TransitionNames;

        public bool JoregMode = false;

        public ObservableCollection<int> ShowSlice = new ObservableCollection<int>(); //which slice of automata is shown ?

        public ObservableCollection<int> ActiveStateIndex = new ObservableCollection<int>();

        public List<int> TargetStateIndex = new List<int>();

        //public List<int> TransitionTimeSettingOut = new List<int>();

        public List<int> TransitionIndex = new List<int>();

        public List<int> TransitionFramesOut = new List<int>();

        public List<int> ElapsedStateTime = new List<int>();

        #endregion fields & pins

        #region variables

        public int x = 0; //mouse koordinaten
        public int y = 0;
        public Point previousPosition;
        public Point holdMousePos;

        State hitState = new State(); //hit detection
        Transition hitTransition = new Transition(); //hit detection
        AutomataRegion hitRegion = new AutomataRegion();
        AutomataRegion hitsizeHandle = new AutomataRegion();

        public List<State> stateList = new List<State>();
        public List<Transition> transitionList = new List<Transition>();
        public List<AutomataRegion> regionList = new List<AutomataRegion>();

        public State selectedState = null;
        public State startConnectionState = null;
        public State targetConnectionState = null;

        public string EnumName = "";
        //public InputAttribute attr;

        public PaintAutomataClass p = new PaintAutomataClass(); // create AutomataPaint Object

        private bool Initialize = true;

        private string dragState = null;

        public string myGUID = Guid.NewGuid().ToString();

        public string licenseOwner = "Automata UI ";

        //Dictionary<string, IIOContainer> FPins = new Dictionary<string, IIOContainer>(); //dynamic pins

        #endregion variables

        #region Debug
        public string debug = "Initialize variables";
        #endregion

        #region Events
        void onSliceChange(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.Invalidate();
        }

        void BindEvents()
        {
            // redraw if you want to see another slice of automata
            ShowSlice.CollectionChanged += onSliceChange;
            ActiveStateIndex.CollectionChanged += onSliceChange;
        }
        #endregion

        #region Constructor and initialisation
        public void OnImportsSatisfied()
        {
            ///
            /// Getting File Version
            ///
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            licenseOwner += fvi.FileVersion;
        }

        public AutomataUI()
        {
            //setup the gui
            InitializeComponent();
            BindEvents();
        }

        void InitializeComponent()
        {
            Controls.Clear(); //clear controls in case init is called multiple times

            //bind events
            MouseMove += Form1_MouseMove; //mouse move event
            MouseDoubleClick += Form1_MouseDoubleClick; //mouse click event
            MouseDown += Form1_MouseDown; //mouse down event
            MouseUp += Form1_MouseUp;

            if (ShowSlice.Count < 1)
            {
                ShowSlice.Add(0);
            }

            Paint += p.PaintAutomata; //paint event
            p.InitAutomataDrawing(); //setup textalignment, arrows

            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

        }

        private void InitSettings()
        {
            //load settings
            if (stateList.Count < 2 && StateXML.Length > 3 && Initialize)
            {
                try
                {
                    stateList = State.DataDeserializeState(StateXML);
                    transitionList = Transition.DataDeserializeTransition(TransitionXML);

                    if (RegionXML.Length > 3) regionList = AutomataRegion.DataDeserializeRegion(RegionXML);
                }
                catch { debug = "Loading XML Graph failed!"; }

                //new enum technique
                //EnumManager.UpdateEnum(myGUID + "_States", stateList[0].Name, stateList.Select(x => x.Name).ToArray());
                stateList.ForEach(state => StatesEnum.AddEntry(state.Name));
                //EnumManager.UpdateEnum(myGUID + "_Transitions", transitionList[0].Name, transitionList.Select(x => x.Name).Distinct().ToArray());
                transitionList.ForEach(transition => TransitionsEnum.AddEntry(transition.Name));
                //EnumManager.UpdateEnum(myGUID + "_Regions", regionList[0].Name, regionList.Select(x => x.Name).ToArray());
                regionList.ForEach(region => RegionsEnum.AddEntry(region.Name));
                //EnumManager.UpdateEnum(myGUID + "_AllTransitions", transitionList[0].Name, transitionList.Select(x => x.Name).ToArray());

                //repair relation
                foreach (Transition transition in transitionList)
                {
                    transition.startState = stateList.First(x => x.ID.Contains(transition.startState.ID));
                    transition.endState = stateList.First(x => x.ID.Contains(transition.endState.ID));
                }

                this.Invalidate();
                previousPosition = MousePosition;
                p.StagePos.X = 0;
                p.StagePos.Y = 0;

                Initialize = false;
            }
        }
        #endregion constructor and init

        #region mouse

        private void Form1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //hit detection for various use
            hitState = stateList.FirstOrDefault(x => x.Bounds.Contains(new Point(this.x, this.y)));
            hitTransition = transitionList.FirstOrDefault(x => x.Bounds.Contains(new Point(this.x, this.y)));
            hitRegion = regionList.FirstOrDefault(x => x.Bounds.Contains(new Point(this.x, this.y)));
            hitsizeHandle = regionList.FirstOrDefault(x => x.SizeHandle.Contains(new Point(this.x, this.y)));

            if (hitsizeHandle != null) hitRegion = null;

            previousPosition = MousePosition; // get mouse position difference 

            holdMousePos = new Point(x, y); // on mouse down hold last position

            //delete transitions or Regions
            if (e.Button == MouseButtons.Middle)
            {
                DeleteTransition(e);
                DeleteRegion(e);
            }

            //hit detection bezier transition
            if (e.Button == MouseButtons.Right)
            {
                int i = 0;
                p.bezierEdit.HighlightTransitionIndex = null;
                p.bezierEdit.highlightTransition = null;

                foreach (GraphicsPath path in p.transitionPaths)
                {
                    if (path.IsOutlineVisible(this.x, this.y, p.greenPen))
                    {
                        p.bezierEdit.HighlightTransitionIndex = i;
                        p.bezierEdit.highlightTransition = transitionList[i];
                        //FLogger.Log(LogType.Debug, "hit transition");
                    }
                    i++;
                }
            }

            if (p.bezierEdit.bezierStart.Contains(new Point(this.x, this.y)) && e.Button == MouseButtons.Left) dragState = "bezierStart";
            if (p.bezierEdit.bezierEnd.Contains(new Point(this.x, this.y)) && e.Button == MouseButtons.Left) dragState = "bezierEnd";


            // Override Active State by CTRL Mouseclick
            if (e.Button == MouseButtons.Left && Form.ModifierKeys == Keys.Control && hitState != null)
            {
                //ActiveStateIndex[ShowSlice[0]] = TargetStateIndex[ShowSlice[0]] = stateList.IndexOf(hitState);
                ActiveStateIndex[ShowSlice[0]] = TargetStateIndex[ShowSlice[0]] = stateList.FindIndex(_hitState => _hitState.ID.Equals(hitState.ID));
                ElapsedStateTime[ShowSlice[0]] = TransitionFramesOut[ShowSlice[0]] = 0;
                this.Invalidate(); //redraw
            }

            // Override Active Transition by CTRL Mouseclick
            if (e.Button == MouseButtons.Left && Form.ModifierKeys == Keys.Control && hitTransition != null)
            {
                //TargetStateIndex[ShowSlice[0]] = stateList.IndexOf(hitTransition.endState); // set target state index
                TargetStateIndex[ShowSlice[0]] = stateList.FindIndex(_hitTransition => _hitTransition.ID.Equals(hitTransition.endState)); // set target state index
                //ActiveStateIndex[ShowSlice[0]] = stateList.IndexOf(hitTransition.startState);
                ActiveStateIndex[ShowSlice[0]] = stateList.FindIndex(_hitTransition => _hitTransition.ID.Equals(hitTransition.startState));


                TransitionFramesOut[ShowSlice[0]] = hitTransition.Frames; // get frames of transition
                //TransitionIndex[ShowSlice[0]] = transitionList.IndexOf(hitTransition); //get transition
                TransitionIndex[ShowSlice[0]] = transitionList.FindIndex(_hitTransition => 
                _hitTransition.endBezierPoint.Equals(hitTransition.endBezierPoint)); //get transition, maybe endBezierPoint is not good for searching
                
                ElapsedStateTime[ShowSlice[0]] = 0; // stop ElapsedStateTimer

                //FLogger.Log(LogType.Debug, "force transition");

                this.Invalidate(); //redraw
            }


            // empty hit ?
            if (hitState == null)
            {
                selectedState = null;
                startConnectionState = null;
            }

            else
            {
                selectedState = hitState; //on click set selected state for dragging

                if (startConnectionState != null && targetConnectionState != null && e.Button == MouseButtons.Left)
                {
                    //Create Transition
                    if (startConnectionState.ID != targetConnectionState.ID) AddTransition(startConnectionState, hitState);

                    startConnectionState = null;
                    targetConnectionState = null;
                    selectedState = null;
                }

                //set connection start
                if (selectedState != null && e.Button == MouseButtons.Right) startConnectionState = hitState;
                else startConnectionState = null;

                //delete state and relevant transitions
                if (selectedState != null && e.Button == MouseButtons.Middle && hitState.ID != "Init") DeleteState(e);

            }

            // hit test for spreadbuttons top left
            for (int i = 0; i < p.Spreadbuttons.Count; i++)
            {
                if (p.Spreadbuttons[i].Contains(new Point(e.X, e.Y)) && e.Button == MouseButtons.Left)
                {
                    ShowSlice[0] = i;
                    break;
                }
            }

        }

        private void Form1_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {

            if (hitState == null) selectedState = startConnectionState = null; //empty interaction states

            if (stateList.Count > 2) StateXML = State.DataSerializeState(stateList); //update config

            if (regionList.Count > 0) RegionXML = AutomataRegion.DataSerializeRegion(regionList); //update region config

            if (dragState != null)
            {
                dragState = null;
                TransitionXML = Transition.DataSerializeTransition(transitionList);
            }

            //new selection rectangle ? create region
            if (p.selectionRectangle.Height != 0)
            {
                CreateRegion();
                p.selectionRectangle.Height = 0;
            }

            hitRegion = null;
            hitsizeHandle = null;
        }

        private void Form1_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            x = Convert.ToInt32((e.X - p.StagePos.X) / p.dpi);
            y = Convert.ToInt32((e.Y - p.StagePos.Y) / p.dpi);
            TransitionFramesOut[0] = 0;

            if (hitState == null && hitTransition == null && e.Button == MouseButtons.Left && ModifierKeys != Keys.Shift) AddState("MyState"); // Add State 

            else if (hitState != null && hitTransition == null) EditState(hitState); //Edit State

            if (hitTransition != null && hitState == null) EditTransition(hitTransition); //Edit Transition

            if (hitState == null && hitTransition == null && hitRegion != null && ModifierKeys == Keys.Shift && e.Button == MouseButtons.Left) EditRegion(hitRegion); // edit region

        }

        private void Form1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            x = Convert.ToInt32((e.X - p.StagePos.X) / p.dpi);
            y = Convert.ToInt32((e.Y - p.StagePos.Y) / p.dpi);

            //hittest states
            hitState = stateList.FirstOrDefault(x => x.Bounds.Contains(new Point(this.x, this.y)));

            #region drag bezier handles
            if (dragState != null && e.Button == MouseButtons.Left)
            {
                //FLogger.Log(LogType.Debug, "bezierHandleStart");
                Lines.EdgePoints myEdgePoints = Lines.GetEdgePoints(State.Center(p.bezierEdit.highlightTransition.startState.Bounds), State.Center(p.bezierEdit.highlightTransition.endState.Bounds), 40, 40, 0.0);
                if (dragState == "bezierStart") p.bezierEdit.highlightTransition.startBezierPoint = new Point(this.x - myEdgePoints.A.X, this.y - myEdgePoints.A.Y);
                if (dragState == "bezierEnd") p.bezierEdit.highlightTransition.endBezierPoint = new Point(this.x - myEdgePoints.B.X, this.y - myEdgePoints.B.Y);
            }
            #endregion

            #region drag things

            //drag stage
            if (selectedState == null && e.Button == MouseButtons.Right)
            {
                Point mousePos = MousePosition;
                int deltaX = (mousePos.X - previousPosition.X);
                int deltaY = (mousePos.Y - previousPosition.Y);
                previousPosition = MousePosition;
                p.StagePos.X += deltaX;
                p.StagePos.Y += deltaY;
            }

            //drag state
            if (selectedState != null && e.Button == MouseButtons.Left && dragState == null)
            {
                selectedState.Move(new Point(Convert.ToInt32(e.X / p.dpi) - (p.StateSize / 2) - Convert.ToInt32(p.StagePos.X / p.dpi), Convert.ToInt32(e.Y / p.dpi) - (p.StateSize / 2) - Convert.ToInt32(p.StagePos.Y / p.dpi)));
            }

            //drag region
            if (selectedState == null && hitRegion != null && e.Button == MouseButtons.Left && hitsizeHandle == null && dragState == null)
            {
                Point mousePos = MousePosition;
                int deltaX = (mousePos.X - previousPosition.X);
                int deltaY = (mousePos.Y - previousPosition.Y);
                previousPosition = MousePosition;
                hitRegion.Bounds = new Rectangle(hitRegion.Bounds.X + deltaX, hitRegion.Bounds.Y + deltaY, hitRegion.Bounds.Width, hitRegion.Bounds.Height);
                hitRegion.SizeHandle = new Rectangle(hitRegion.SizeHandle.X + deltaX, hitRegion.SizeHandle.Y + deltaY, 10, 10);
            }

            //drag size

            if(e.Button == MouseButtons.Left && hitsizeHandle != null)
            {
                Point mousePos = MousePosition;
                int deltaX = (mousePos.X - previousPosition.X);
                int deltaY = (mousePos.Y - previousPosition.Y);
                previousPosition = MousePosition;


                int sizeX = hitsizeHandle.SizeHandle.X + deltaX;
                int sizeY = hitsizeHandle.SizeHandle.Y + deltaY;

                if ((sizeX - hitsizeHandle.Bounds.X + 10) < 100) sizeX = hitsizeHandle.Bounds.X + 90;
                if ((sizeY - hitsizeHandle.Bounds.Y + 10) < 100) sizeY = hitsizeHandle.Bounds.Y + 90;

                hitsizeHandle.SizeHandle = new Rectangle(sizeX, sizeY, 10,10);
                hitsizeHandle.Bounds = new Rectangle(hitsizeHandle.Bounds.X, hitsizeHandle.Bounds.Y, sizeX - hitsizeHandle.Bounds.X +10, sizeY - hitsizeHandle.Bounds.Y+10);

            }


            #endregion

            #region startConnection

            if (startConnectionState != null && hitState != null)
            {
                targetConnectionState = hitState;
            }
            else targetConnectionState = null;
            #endregion

            SetSelectionRectangle(e);

            this.Invalidate(); //redraw
        }

        #endregion mouse

        #region Management
        private void AddTransition(State startState, State endState)
        {

            bool exists = false;

            //check if the transition already exists
            foreach (Transition transition in transitionList) // Loop through List with foreach.
            {
                if (transition.startState.ID == startState.ID
                && transition.endState.ID == endState.ID)
                {
                    exists = true;   // achtung test, war vorher true
                    break;
                }
                else exists = false;
            }

            // transition does not exist ? ok, create it
            if (exists == false || FAllowMultiple[0])
            {
                string input = "to" + endState.Name; //dialog text
                int frames = 1;
                bool pingpong = false;
                {
                    if (PaintAutomataClass.Dialogs.ShowTransitionDialog(ref input, ref frames, ref pingpong, "Add Transition", p.dpi) == DialogResult.OK)
                    {
                        //add transition
                        transitionList.Add(new Transition()
                        {
                            Name = input,
                            Frames = frames,
                            startState = startState,
                            endState = endState,
                            IsPingPong = pingpong,
                            startBezierPoint = new Point(0, 0), //angle lenght
                            endBezierPoint = new Point(0, 0)
                        });
                        //update config
                        UpdateTransitionConfigs();
                    }
                }
            }


        }

        private void EditTransition(Transition transition)
        {
            string input = transition.Name;
            int frames = transition.Frames;
            bool pingpong = transition.IsPingPong;

            if (PaintAutomataClass.Dialogs.ShowTransitionDialog(ref input, ref frames, ref pingpong, "Edit Transition", p.dpi) == DialogResult.OK)
            {

                transition.Name = input;
                transition.Frames = frames;
                transition.IsPingPong = pingpong;
                this.Invalidate();

                //update transition config
                UpdateTransitionConfigs();
            }
        }

        private void DeleteTransition(System.Windows.Forms.MouseEventArgs e)
        {
            transitionList.RemoveAll(x => x.Bounds.Contains(new Point(this.x, this.y)));
            p.bezierEdit.HighlightTransitionIndex = null;
            p.bezierEdit.highlightTransition = null;
            UpdateTransitionConfigs();
        }

        private void AddState(string input)
        {
            int frames = 0;
            if (PaintAutomataClass.Dialogs.ShowInputDialog(ref input, ref frames, "Add State", p.dpi) == DialogResult.OK)
            {
                //add state to state list
                stateList.Add(new State()
                {
                    ID = Automata.Data.State.RNGCharacterMask(),
                    Name = UppercaseFirst(input),
                    Frames = frames,
                    Bounds = new Rectangle(new Point(x - (p.StateSize / 2), y - (p.StateSize / 2)), new Size(p.StateSize, p.StateSize))
                });

                UpdateStateConfigs(); // update JSON,Enums and Redraw
            }
        }

        private void EditState(State state)
        {

            string input = state.Name;
            int frames = state.Frames;
            if (input != "Init") //edit state unless its init
            {
                if (PaintAutomataClass.Dialogs.ShowInputDialog(ref input, ref frames, "Edit State", p.dpi) == DialogResult.OK)
                {
                    state.Name = UppercaseFirst(input);
                    state.Frames = frames;
                    UpdateStateConfigs(); // update JSON,Enums and Redraw
                }
            }
        }

        private void DeleteState(System.Windows.Forms.MouseEventArgs e)
        {
            stateList.RemoveAll(x => x.Bounds.Contains(new Point(this.x, this.y)));
            for (int i = transitionList.Count - 1; i >= 0; i--)
            {
                Transition transition = new Transition();
                transition = transitionList.ElementAt(i);

                if (hitState.ID == transition.startState.ID || hitState.ID == transition.endState.ID)
                {
                    transitionList.RemoveAt(i);
                }
            }

            ActiveStateIndex[0] = 0; //set active State
            TargetStateIndex[0] = 0; //set active TargetState

            UpdateStateConfigs(); // update JSON,Enums and Redraw
            UpdateTransitionConfigs();
        }

        private void UpdateTransitionConfigs()
        {
            debug = "Enter Update Transition Configs";
            // Update Config Pin if there is a change
            TransitionXML = Transition.DataSerializeTransition(transitionList);
            
            //unnecessary elements 
            //TransitionTimeSettingOut.Clear();
            //TransitionNames.Add("Reset To Default State"); // Default Reset Transition to Init
            //foreach (Transition transition in transitionList) // Loop through List with foreach.
            //{
            //    TransitionNames.Add(transition.Name);
            //    TransitionTimeSettingOut.Add(transition.Frames);
            //}

            debug = "Prepare for Transitions Enum Clearing";

            TransitionsEnum.Clear();
            transitionList.ForEach(transition => TransitionsEnum.AddEntry(transition.Name));
            debug = transitionList.Count.ToString();

            //upper part seems to be clearer

            //if (transitionList.Count > 0)
            //{
            //    //new enum technique
            //    //EnumManager.UpdateEnum(myGUID + "_Transitions", transitionList[0].Name, transitionList.Select(x => x.Name).Distinct().ToArray());
            //    //EnumManager.UpdateEnum(myGUID + "_AllTransitions", transitionList[0].Name, transitionList.Select(x => x.Name).ToArray());
            //} else
            //{
            //    //EnumManager.UpdateEnum(myGUID + "_Transitions", "", transitionList.Select(x => x.Name).Distinct().ToArray());
            //    //EnumManager.UpdateEnum(myGUID + "_AllTransitions", "", transitionList.Select(x => x.Name).ToArray());
            //}

        }

        private void UpdateStateConfigs()
        {
            this.Invalidate();
            //update Default State Enum
            //EnumManager.UpdateEnum(EnumName, stateList[0].Name, stateList.Select(x => x.Name).ToArray());
            StateXML = State.DataSerializeState(stateList); //save config

            //new enum technique
            //EnumManager.UpdateEnum(myGUID + "_States", stateList[0].Name, stateList.Select(x => x.Name).ToArray());
            StatesEnum.Clear();
            stateList.ForEach(state => StatesEnum.AddEntry(state.Name));
            //FLogger.Log(LogType.Debug, "update enums state");
        }

        public void SetSelectionRectangle(System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && ModifierKeys == Keys.Shift && hitRegion == null && hitsizeHandle == null)
            {

                //negative and positive rectangle since drawing doesnt work with negative values
                var rc = new Rectangle(
                                        Math.Min(holdMousePos.X, x),
                                        Math.Min(holdMousePos.Y, y),
                                        Math.Abs(x - holdMousePos.X),
                                        Math.Abs(y - holdMousePos.Y));
                p.selectionRectangle = rc;
            }
        }

        private void CreateRegion()
        {
            string input = "Region";

            if (PaintAutomataClass.Dialogs.RegionDialog(ref input, "Create Region", p.dpi) == DialogResult.OK)
            {
                //add state to state list
                regionList.Add(new AutomataRegion()
                {
                    Name = input,
                    Bounds = new Rectangle(p.selectionRectangle.Location, p.selectionRectangle.Size),
                    SizeHandle = new Rectangle(p.selectionRectangle.X + p.selectionRectangle.Width - 10,
                    p.selectionRectangle.Y +p.selectionRectangle.Height - 10,
                    10,10)
                });
                this.Invalidate(); //redraw
                UpdateRegionsConfigs();
            }
          
        }

        private void UpdateRegionsConfigs()
        {
            // Update Config Pin if there is a change



            if (regionList.Count > 0)
            {
                RegionXML = AutomataRegion.DataSerializeRegion(regionList);
                //EnumManager.UpdateEnum(myGUID + "_Regions", regionList[0].Name, regionList.Select(x => x.Name).ToArray());
                RegionsEnum.Clear();
                regionList.ForEach(region => RegionsEnum.AddEntry(region.Name));
            }
                           
            else
            {
                RegionXML = "";
                //EnumManager.UpdateEnum(myGUID + "_Regions", "", regionList.Select(x => x.Name).ToArray());
                RegionsEnum.Clear();
            }

        }

        private void EditRegion(AutomataRegion region)
        {

            string input = region.Name;


            if (PaintAutomataClass.Dialogs.RegionDialog(ref input, "Edit Region", p.dpi) == DialogResult.OK)
            {
                region.Name = input;
                this.Invalidate(); //redraw
                UpdateRegionsConfigs();
            }
            
        }

        private void DeleteRegion(System.Windows.Forms.MouseEventArgs e)
        {
            if (hitState == null && hitTransition == null)
            {
                regionList.RemoveAll(x => x.Bounds.Contains(new Point(this.x, this.y)));
                UpdateRegionsConfigs();
            }
        }

        #endregion Management

        public void ResetToState (int ii, StatesEnum statesEnum)
        {
            ActiveStateIndex[ii] = statesEnum.SelectedIndex();
            TargetStateIndex[ii] = statesEnum.SelectedIndex();
            ElapsedStateTime[ii] = 0; // Reset Timer
            TransitionFramesOut[ii] = 0; // Reset Timer
            this.Invalidate();
        }

        public void TriggerTransition(int ii, int ResetStateIndex, TransitionsEnum transitionsEnum)
        {
            //FLogger.Log(LogType.Debug,pin.ToString());
            //if (transitionsEnum.Value == "Reset To Default State")
            ////if (TransitionName == "Reset To Default State") // Reset to Init State
            //{

            //    // Get Enum Index From Default State and Set Active State
            //    ActiveStateIndex[ii] = ResetStateIndex;
            //    TargetStateIndex[ii] = ResetStateIndex;
            //    ElapsedStateTime[ii] = 0; // Reset Timer
            //    TransitionFramesOut[ii] = 0; // Reset Timer
            //    this.Invalidate();
            //}
            //else
            //{
                //Find Transition
                int i = 0;
                foreach (Transition transition in transitionList)
                {
                    // standard transitions
                    if (transition.Name == transitionsEnum.Value &&
                    //if (transition.Name == TransitionName &&
                        transition.startState.ID == stateList.ElementAt(ActiveStateIndex[ii]).ID &&
                        TransitionFramesOut[ii] == 0 &&
                        ElapsedStateTime[ii] >= transition.startState.Frames)
                    {
                        TargetStateIndex[ii] = stateList.FindIndex(endState => endState.ID.Equals(transition.endState.ID)); // set target state index
                        TransitionFramesOut[ii] = transition.Frames; // get frames of transition
                        TransitionIndex[ii] = i; //get transition
                        ElapsedStateTime[ii] = 0; // stop ElapsedStateTimer
                        this.Invalidate(); //redraw

                        break;
                    }

                    //pingpong transitions - return to startstate , previous test covers transition to targetstate
                    if (transition.Name == transitionsEnum.Value &&
                    //if (transition.Name == TransitionName &&
                        transition.endState.ID == stateList.ElementAt(ActiveStateIndex[ii]).ID &&
                        TransitionFramesOut[ii] == 0 &&
                        transition.IsPingPong &&
                        ElapsedStateTime[ii] >= transition.endState.Frames)
                    {
                        //TargetStateIndex[ii] = stateList.IndexOf(transition.startState); // set target state index
                        TargetStateIndex[ii] = stateList.FindIndex(_startState => _startState.ID.Equals(transition.startState.ID)); // set target state index
                        TransitionFramesOut[ii] = transition.Frames; // get frames of transition, hier war +1
                        TransitionIndex[ii] = i; //get transition
                        ElapsedStateTime[ii] = 0; // stop ElapsedStateTimer
                        this.Invalidate(); //redraw

                        break;
                    }
                    i++;
                }
            //}
        }

        static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public void Evaluate(int SpreadMax)
        {
            InitSettings(); // Load previous setting and setup certain variables

            if (FSpreadCount <= 0) FSpreadCount = 1;
            if (FSpreadCount > prevSpreadCount)
            {
                for (int i = 0; i < FSpreadCount - prevSpreadCount; i++)
                {
                    ActiveStateIndex.Add(0);
                    TargetStateIndex.Add(0);
                    TransitionIndex.Add(0);
                    TransitionFramesOut.Add(0);
                    ElapsedStateTime.Add(0);
                }
                prevSpreadCount = FSpreadCount;
            }
            else if (FSpreadCount < prevSpreadCount)
            {
                for (int i = prevSpreadCount; i > FSpreadCount; i--)
                {
                    ActiveStateIndex.RemoveAt(i - 1);
                    TargetStateIndex.RemoveAt(i - 1);
                    TransitionIndex.RemoveAt(i - 1);
                    TransitionFramesOut.RemoveAt(i - 1);
                    ElapsedStateTime.RemoveAt(i - 1);
                }
                prevSpreadCount = FSpreadCount;

            }

            //need to rewrite that part on VL side?

            //#region TriggerTransitions
            //for (int ii = 0; ii < FSpreadCount; ii++) //spreadable loop 01
            //{
            //    foreach (var pin in FPins)
            //    {
            //        var diffpin = pin.Value.RawIOObject as IDiffSpread<bool>;
            //        if (diffpin[ii] == true && diffpin.SliceCount != 0) //diffpin.IsChanged && JONAS WUNSCHKONZERT
            //        {
            //            TriggerTransition(pin.Key, ii, 0);
            //        }
            //    }
            //}
            //#endregion TriggerTransitions

            #region TimingAndIndices

            for (int ii = 0; ii < FSpreadCount; ii++) //spreadable loop 02
            {
                // set active Transition,State and Timers 

                if (ActiveStateIndex[ii] != TargetStateIndex[ii] && TransitionFramesOut[ii] != 0) // solange target und active ungleich sind, läuft die transitions
                {
                    TransitionFramesOut[ii] -= 1; // run Transition Timer 
                    
                }

                //passiert nur einmal
                if (TransitionFramesOut[ii] == 0 && ElapsedStateTime[ii] == 0) //solange transition time und elapsedtime 0 sind, setze target und active gleich
                {
                    ActiveStateIndex[ii] = TargetStateIndex[ii]; // after transition set activestate to targetstate
                    TransitionIndex[ii] = transitionList.Count - 1;
                    this.Invalidate(); //redraw
                    
                }

                //FLogger.Log(LogType.Debug, "Transition Ends");

                if (TransitionFramesOut[ii] == 0) ElapsedStateTime[ii] += 1; // Run State Timer when TransitionTimer is 0
            }

            if (JoregMode) p.JoregMode(this, true);   //Joreg Mode
            else if (!JoregMode) p.JoregMode(this, false);

            #endregion TimingAndIndices
        }
    }
}
