﻿using System;
using System.Linq;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using System.IO;
using System.Net;
using ColossalFramework.Importers;
using System.Collections;
using Mapper.OSM;
using ColossalFramework.Plugins;

namespace Mapper
{
    public class MapperWindow7 : UIPanel
    {
        UILabel title;

        UITextField pathTextBox;
        UILabel pathTextBoxLabel;
        UIButton loadMapButton;

        UITextField coordinates;
        UILabel coordinatesLabel;
        UIButton loadAPIButton;
        UIButton loadTerrainParty;

        UIButton pedestriansCheck;
        UILabel pedestrianLabel;

        UIButton roadsCheck;
        UILabel roadsLabel;

        UIButton highwaysCheck;
        UILabel highwaysLabel;

        UITextField scaleTextBox;
        UILabel scaleTextBoxLabel;

        UITextField tolerance;
        UILabel toleranceLabel;

        UITextField curveTolerance;
        UILabel curveToleranceLabel;

        UITextField tiles;
        UILabel tilesLabel;

        UILabel errorLabel;

        UIButton okButton;

        private UITextField mapquestKey;
        private UILabel mapquestKeyLabel;

        public ICities.LoadMode mode;
        RoadMaker2 roadMaker;
        private byte[] nodesXml;
        private byte[] waysXml;
        private osmBounds ob;

        bool nodesLoaded = false;
        bool waysLoaded = false;

        bool createRoads;
        int currentIndex = 0;
        bool peds = false;
        bool roads = true;
        bool highways = true;
        private byte[] m_LastHeightmap16;

        public override void Awake()
        {
            this.isInteractive = true;
            this.enabled = true;

            width = 500;

            title = AddUIComponent<UILabel>();

            coordinates = AddUIComponent<UITextField>();
            coordinatesLabel = AddUIComponent<UILabel>();
            loadAPIButton = AddUIComponent<UIButton>();
            loadTerrainParty = AddUIComponent<UIButton>();

            pathTextBox = AddUIComponent<UITextField>();
            pathTextBoxLabel = AddUIComponent<UILabel>();
            loadMapButton = AddUIComponent<UIButton>();

            pedestriansCheck = AddUIComponent<UIButton>();
            pedestrianLabel = AddUIComponent<UILabel>();
            roadsCheck = AddUIComponent<UIButton>();
            roadsLabel = AddUIComponent<UILabel>();
            highwaysCheck = AddUIComponent<UIButton>();
            highwaysLabel = AddUIComponent<UILabel>();

            scaleTextBox = AddUIComponent<UITextField>();
            scaleTextBoxLabel = AddUIComponent<UILabel>();


            tolerance = AddUIComponent<UITextField>();
            toleranceLabel = AddUIComponent<UILabel>();

            curveTolerance = AddUIComponent<UITextField>();
            curveToleranceLabel = AddUIComponent<UILabel>();

            tiles = AddUIComponent<UITextField>();
            tilesLabel = AddUIComponent<UILabel>();

            mapquestKey = AddUIComponent<UITextField>();
            mapquestKeyLabel = AddUIComponent<UILabel>();

            errorLabel = AddUIComponent<UILabel>();

            okButton = AddUIComponent<UIButton>();

            base.Awake();
        }

        public override void Start()
        {
            base.Start();

            relativePosition = new Vector3(396, 58);
            backgroundSprite = "MenuPanel2";
            isInteractive = true;
            //this.CenterToParent();
            SetupControls();
        }

        public void SetupControls()
        {
            title.text = "Open Street Map Import";
            title.relativePosition = new Vector3(15, 15);
            title.textScale = 0.9f;
            title.size = new Vector2(200, 30);
            var vertPadding = 30;
            var x = 15;
            var y = 50;

            SetLabel(pedestrianLabel, "Pedestrian Paths", x, y);
            SetButton(pedestriansCheck, peds.ToString(), x + 114, y);
            pedestriansCheck.eventClick += pedestriansCheck_eventClick;
            x += 190;
            SetLabel(roadsLabel, "Roads", x, y);
            SetButton(roadsCheck, "True", x + 80, y);
            roadsCheck.eventClick += roadsCheck_eventClick;
            x += 140;
            SetLabel(highwaysLabel, "Highways", x, y);
            SetButton(highwaysCheck, "True", x + 80, y);
            highwaysCheck.eventClick += highwaysCheck_eventClick;

            x = 15;
            y += vertPadding;

            SetLabel(scaleTextBoxLabel, "Scale", x, y);
            SetTextBox(scaleTextBox, "1", x + 120, y);
            y += vertPadding;


            SetLabel(toleranceLabel, "Tolerance", x, y);
            SetTextBox(tolerance, "2", x + 120, y);
            y += vertPadding;

            SetLabel(curveToleranceLabel, "Curve Tolerance", x, y);
            SetTextBox(curveTolerance, "1", x + 120, y);
            y += vertPadding;

            SetLabel(tilesLabel, "Tiles to Boundary", x, y);
            SetTextBox(tiles, "2.5", x + 120, y);
            y += vertPadding + 12;

            SetLabel(pathTextBoxLabel, "Path", x, y);
            SetTextBox(pathTextBox,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "map"), x + 120, y);
            y += vertPadding - 5;
            SetButton(loadMapButton, "Load OSM From File", y);
            loadMapButton.eventClick += LoadOsmFileHandleMapButtonEventClick;
            y += vertPadding + 5;

            SetLabel(coordinatesLabel, "Bounding Box", x, y);
            SetTextBox(coordinates, "-70.347773,43.535375,-70.124011,43.759138", x + 120, y);
            y += vertPadding - 5;

            // SetButton(loadTerrainParty, "Load From terrain.party", y);
            // loadTerrainParty.tooltip = "Load terrain height data from terrain.party.";
            // loadTerrainParty.eventClick += loadTerrainParty_eventClick;
            // y += vertPadding + 5;

            SetButton(loadAPIButton, "Load From OpenStreetMap", y);
            loadAPIButton.tooltip = "Load road map data from OpenStreetMap";
            loadAPIButton.eventClick += loadAPIButton_eventClick;
            y += vertPadding + 5;

            SetLabel(errorLabel, "No OSM data loaded.", x, y);
            errorLabel.textScale = 0.6f;
            y += vertPadding + 12;

            SetButton(okButton, "Make Roads", y);
            okButton.eventClick += okButton_eventClick;
            okButton.Disable();

            height = y + vertPadding;
        }

        private void loadTerrainParty_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            try
            {
                decimal startLat = 0M;
                decimal startLon = 0M;
                decimal endLat = 0M;
                decimal endLon = 0M;
                var sc = double.Parse(scaleTextBox.text);
                if (!GetCoordinates(ref startLon, ref startLat, ref endLon, ref endLat))
                {
                    return;
                }

                var client = new WebClient();
                client.Headers.Add("user-agent", "Cities Skylines Mapping Mod v1");
                var terrainData = client.DownloadData(
                    new System.Uri(string.Format(
                        "http://terrain.party/api/export?box={0},{1},{2},{3}&heightmap=merged", endLon, endLat,
                        startLon,
                        startLat)));
                errorLabel.text = "Downloading map from Terrain.Party...";
                ProcessHeightMap(terrainData);
            }
            catch (Exception ex)
            {
                errorLabel.text = ex.ToString();
            }
        }

        private IEnumerator LoadHeightMap(byte[] heightmap)
        {
            Singleton<TerrainManager>.instance.SetHeightMap16(heightmap);
            errorLabel.text = "Terrain Loaded";
            yield return null;
        }

        private void ProcessHeightMap(byte[] result)
        {
            try
            {
                var image = new Image(result);
                image.Convert(Image.kFormatAlpha16);
                if (image.width != 1081 || image.height != 1081)
                {
                    if (!image.Resize(1081, 1081))
                    {
                        errorLabel.text = string.Concat(new object[]
                        {
                            "Resize not supported: ",
                            image.format,
                            "-",
                            image.width,
                            "x",
                            image.height,
                            " Expected: ",
                            1081,
                            "x",
                            1081
                        });
                        return;
                    }
                }

                m_LastHeightmap16 = image.GetPixels();
                SimulationManager.instance.AddAction(LoadHeightMap(m_LastHeightmap16));
            }
            catch (Exception ex)
            {
                errorLabel.text = ex.ToString();
            }
        }

        private void highwaysCheck_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            highways = !highways;
            highwaysCheck.text = highways.ToString();
        }

        private void roadsCheck_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            roads = !roads;
            roadsCheck.text = roads.ToString();
        }

        private void pedestriansCheck_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            peds = !peds;
            pedestriansCheck.text = peds.ToString();
        }

        private void loadAPIButton_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            Debug.Log("loadAPIButton_eventClick");
            try
            {
                Debug.Log("GetBounds()");
                ob = GetBounds();

                Debug.Log("new OpenStreeMapFrRequest");
                var streetMapRequest = new OpenStreeMapFrRequest(ob);

                Debug.Log("DebugOutputPanel.AddMessage");
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, streetMapRequest.NodeRequestUrl);
                Debug.Log("DebugOutputPanel.AddMessage");
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, streetMapRequest.WaysRequestUrl);

                Debug.Log("new WebClient()");
                var nodesWebClient = new WebClient();
                var nodeResponseData = nodesWebClient.DownloadData(new Uri(streetMapRequest.NodeRequestUrl));
                NodesWebClientCallback(nodeResponseData);

                var waysWebClient = new WebClient();
                var waysResponseData = waysWebClient.DownloadData(new Uri(streetMapRequest.WaysRequestUrl));
                WaysWebClientCallback(waysResponseData);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
                errorLabel.text = ex.ToString();
            }
        }

        private void NodesWebClientCallback(byte[] result)
        {
            Debug.Log("NodesWebClientCallback()");
            try
            {
                nodesXml = result;
                errorLabel.text = string.Format("{0} Data Loaded.", "Node");
                nodesLoaded = true;
                if (nodesLoaded && waysLoaded)
                {
                    errorLabel.text = "OSM Data Loaded Successfully";
                    okButton.Enable();
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
        }

        private void WaysWebClientCallback(byte[] result)
        {
            try
            {
                waysXml = result;
                errorLabel.text = string.Format("{0} Data Loaded.", "Ways");
                waysLoaded = true;
                if (nodesLoaded && waysLoaded)
                {
                    errorLabel.text = "OSM Data Loaded Successfully";
                    okButton.Enable();
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
        }

        private bool GetCoordinates(ref decimal startLon, ref decimal startLat, ref decimal endLon, ref decimal endLat)
        {
            var text = coordinates.text.Trim();
            var split = text.Split(new string[] {","}, StringSplitOptions.RemoveEmptyEntries);
            decimal midLon = 0M;
            decimal midLat = 0M;

            if (split.Count() == 2)
            {
                if (!decimal.TryParse(split[0], out midLon) || !decimal.TryParse(split[1], out midLat))
                {
                    errorLabel.text = "Coordinates must be numbers.";
                    return false;
                }
            }
            else if (split.Count() == 4)
            {
                if (!decimal.TryParse(split[0], out endLon) || !decimal.TryParse(split[1], out startLat) ||
                    !decimal.TryParse(split[2], out startLon) || !decimal.TryParse(split[3], out endLat))
                {
                    errorLabel.text = "Coordinates must be numbers.";
                    return false;
                }

                midLon = (endLon + startLon) / 2M;
                midLat = (endLat + startLat) / 2M;
            }
            else
            {
                errorLabel.text =
                    "Coordinate format wrong! Input either one or two sets of coordinates seperated by commas.";
                return false;
            }

            return true;
        }

        private OSM.osmBounds GetBounds()
        {
            decimal startLat = 0M;
            decimal startLon = 0M;
            decimal endLat = 0M;
            decimal endLon = 0M;

            if (!GetCoordinates(ref startLon, ref startLat, ref endLon, ref endLat))
            {
                return null;
            }

            var ob = new osmBounds();
            ob.minlon = startLon;
            ob.minlat = startLat;
            ob.maxlon = endLon;
            ob.maxlat = endLat;
            return ob;
        }

        private void LoadOsmFileHandleMapButtonEventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            var ob = GetBounds();


            var path = pathTextBox.text.Trim();
            if (!File.Exists(path))
            {
                path += ".osm";
                if (!File.Exists(path))
                {
                    errorLabel.text = "Cannot find osm file: " + path;
                    return;
                }
            }

            try
            {
                var osm = new OSMInterface(ob, pathTextBox.text.Trim(), double.Parse(scaleTextBox.text.Trim()),
                    double.Parse(tolerance.text.Trim()), double.Parse(curveTolerance.text.Trim()),
                    double.Parse(tiles.text.Trim()));
                currentIndex = 0;
                roadMaker = new RoadMaker2(osm);
                errorLabel.text = "File Loaded.";
                okButton.Enable();
                loadMapButton.Disable();
                loadAPIButton.Disable();
            }
            catch (Exception ex)
            {
                errorLabel.text = ex.ToString();
            }
        }

        private void SetButton(UIButton okButton, string p1, int x, int y)
        {
            okButton.text = p1;
            okButton.normalBgSprite = "ButtonMenu";
            okButton.hoveredBgSprite = "ButtonMenuHovered";
            okButton.disabledBgSprite = "ButtonMenuDisabled";
            okButton.focusedBgSprite = "ButtonMenuFocused";
            okButton.pressedBgSprite = "ButtonMenuPressed";
            okButton.size = new Vector2(50, 18);
            okButton.relativePosition = new Vector3(x, y - 3);
            okButton.textScale = 0.8f;
        }

        private void SetButton(UIButton okButton, string p1, int y)
        {
            okButton.text = p1;
            okButton.normalBgSprite = "ButtonMenu";
            okButton.hoveredBgSprite = "ButtonMenuHovered";
            okButton.disabledBgSprite = "ButtonMenuDisabled";
            okButton.focusedBgSprite = "ButtonMenuFocused";
            okButton.pressedBgSprite = "ButtonMenuPressed";
            okButton.size = new Vector2(260, 24);
            okButton.relativePosition = new Vector3((int) (width - okButton.size.x) / 2, y);
            okButton.textScale = 0.8f;
        }

        private void SetCheckBox(UICustomCheckbox3 pedestriansCheck, int x, int y)
        {
            pedestriansCheck.IsChecked = true;
            pedestriansCheck.relativePosition = new Vector3(x, y);
            pedestriansCheck.size = new Vector2(13, 13);
            pedestriansCheck.Show();
            pedestriansCheck.color = new Color32(185, 221, 254, 255);
            pedestriansCheck.enabled = true;
            pedestriansCheck.spriteName = "AchievementCheckedFalse";
            pedestriansCheck.eventClick +=
                (component, param) => { pedestriansCheck.IsChecked = !pedestriansCheck.IsChecked; };
        }

        private void SetTextBox(UITextField scaleTextBox, string p, int x, int y)
        {
            scaleTextBox.relativePosition = new Vector3(x, y - 4);
            scaleTextBox.horizontalAlignment = UIHorizontalAlignment.Left;
            scaleTextBox.text = p;
            scaleTextBox.textScale = 0.8f;
            scaleTextBox.color = Color.black;
            scaleTextBox.cursorBlinkTime = 0.45f;
            scaleTextBox.cursorWidth = 1;
            scaleTextBox.selectionBackgroundColor = new Color(233, 201, 148, 255);
            scaleTextBox.selectionSprite = "EmptySprite";
            scaleTextBox.verticalAlignment = UIVerticalAlignment.Middle;
            scaleTextBox.padding = new RectOffset(5, 0, 5, 0);
            scaleTextBox.foregroundSpriteMode = UIForegroundSpriteMode.Fill;
            scaleTextBox.normalBgSprite = "TextFieldPanel";
            scaleTextBox.hoveredBgSprite = "TextFieldPanelHovered";
            scaleTextBox.focusedBgSprite = "TextFieldPanel";
            scaleTextBox.size = new Vector3(width - 120 - 30, 20);
            scaleTextBox.isInteractive = true;
            scaleTextBox.enabled = true;
            scaleTextBox.readOnly = false;
            scaleTextBox.builtinKeyNavigation = true;
        }

        private void SetLabel(UILabel pedestrianLabel, string p, int x, int y)
        {
            pedestrianLabel.relativePosition = new Vector3(x, y);
            pedestrianLabel.text = p;
            pedestrianLabel.textScale = 0.8f;
            pedestrianLabel.size = new Vector3(120, 20);
        }

        private void okButton_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            Debug.Log("okButton_eventClick");
            var scale = double.Parse(scaleTextBox.text.Trim());
            var tt = double.Parse(tiles.text.Trim());
            var osm = new OSMInterface(ob, nodesXml, waysXml, scale, double.Parse(tolerance.text.Trim()),
                double.Parse(curveTolerance.text.Trim()), tt);
            currentIndex = 0;
            roadMaker = new RoadMaker2(osm);
            loadMapButton.Disable();
            loadAPIButton.Disable();

            if (roadMaker != null)
            {
                createRoads = !createRoads;
            }
        }

        public override void Update()
        {
            if (createRoads)
            {
                var pp = peds;
                var rr = roads;
                var hh = highways;

                for (int i = 0; i < 10; i++)
                {
                    if (currentIndex < roadMaker.osm.ways.Count())
                    {
                        SimulationManager.instance.AddAction(roadMaker.Make(currentIndex, pp, rr, hh));
                        currentIndex += 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (currentIndex < roadMaker.osm.ways.Count())
                {
                    SimulationManager.instance.AddAction(roadMaker.Make(currentIndex, pp, rr, hh));
                    currentIndex += 1;
                    var instance = Singleton<NetManager>.instance;
                    errorLabel.text = String.Format("Making road {0} out of {1}. Nodes: {2}. Segments: {3}",
                        currentIndex, roadMaker.osm.ways.Count(), instance.m_nodeCount, instance.m_segmentCount);
                }
            }

            if (roadMaker != null && currentIndex == roadMaker.osm.ways.Count())
            {
                errorLabel.text = "Done.";
                createRoads = false;
            }

            base.Update();
        }
    }

    public class UICustomCheckbox3 : UISprite
    {
        public bool IsChecked { get; set; }

        public override void Start()
        {
            base.Start();
            IsChecked = true;
            spriteName = "AchievementCheckedTrue";
        }

        public override void Update()
        {
            base.Update();
            spriteName = IsChecked ? "AchievementCheckedTrue" : "AchievementCheckedFalse";
        }
    }
}