using Microsoft.Azure.SpatialAnchors;
using System;
using System.Threading.Tasks;
using Windows.Perception.Spatial;

namespace StereoKit.Samples.AzureSpatialAnchors
{
    /// <summary>
    /// This class renders our ASA sample.
    /// To use Azure Spatial Anchors we need to add the nuget package <see cref="https://www.nuget.org/packages/Microsoft.Azure.SpatialAnchors.WinRT/"/>.
    /// To get a more detailed info around ASA, see <see cref="https://docs.microsoft.com/en-us/azure/spatial-anchors/how-tos/create-locate-anchors-unity"/> for a more detailed example, based on Unity, but with 
    /// all the detailed C# code that can be easily adopted for StereoKit.
    /// 
    /// Please be aware that for use Space perception, you need to enable it on the app manifest.
    /// </summary>
    class ASADemoScene
    {
        /// <summary>
        /// We need ao <see cref="CloudSpatialAnchorSession"/> to create, locate and manage spatial anchors.
        /// For more details please look at the official documentation at <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.spatialanchors.cloudspatialanchorsession?view=spatialanchors-dotnet"/>.
        /// </summary>
        CloudSpatialAnchorSession _cloudSession;

        /// <summary>
        /// Hold messages to show to the user
        /// </summary>
        private string _feedbackMessage;

        /// <summary>
        /// The 3D model that we will be playing with.
        /// </summary>
        Model _helmetModel = Model.FromFile("DamagedHelmet.gltf");

        /// <summary>
        /// Here is the <see cref="_helmetModel"/> initialy located.
        /// </summary>
        Pose _helmetPose = new Pose(0, -0.1f, -0.5f, Quat.LookDir(-Vec3.Forward));

        /// <summary>
        /// Holds how is the ASA Menu placed on the scene
        /// </summary>
        Pose _asaMenuPose = new Pose(-0.2f, -0.1f, -0.5f, Quat.LookDir(-Vec3.Forward));

        /// <summary>
        /// What are we doing with the ASA Cloud Session
        /// </summary>
        enum ASASessionState
        {
            NOT_STARTED,
            IDLE,
            LOADING_ANCHORS,
            SAVING_ANCHOR,
            DELETING_ANCHOR,
        }

        /// <summary>
        /// Are we saving the anchor?
        /// </summary>
        ASASessionState _sessionState = ASASessionState.NOT_STARTED;

        /// <summary>
        /// If a Spatial Anchor was located, lets keep it so that we can do some actions with it.
        /// </summary>
        CloudSpatialAnchor _locatedAnchor;

        /// <summary>
        /// What parameters will be used to locate the anchors.
        /// </summary>
        AnchorLocateCriteria _anchorLocateCriteria;

        /// <summary>
        /// Called the first time to initialize our scene.
        /// </summary>
        internal void Initialize()
        {
        }

        #region ASA events
        /// <summary>
        /// Whenever a change happen on the cloud session this event is called. 
        /// This is triggered, for instance, every time the session improves its understanding of your surrounding.
        /// We can for instance use this event to see if we already have enough spatial information to create anchors.
        /// </summary>
        private void OnCloudSessionUpdated(object sender, SessionUpdatedEventArgs args)
        {
            Log.Info("The Cloud Session was updated.");
        }

        /// <summary>
        /// Whenever spatial are located or not, this event will be triggered after it tried to locate all provided anchors.
        /// </summary>
        private void OnLocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs args)
        {
            _sessionState = ASASessionState.IDLE;
            Log.Info("The Cloud Session has finished locating anchors.");
        }

        /// <summary>
        /// Whenever anchors are located in the space, this event is triggered. 
        /// personal note: It seems that the documentation is not up-to-date, has this event should also be triggered when no anchors are located, but that is not the case.
        /// "The NotLocated exists for legacy reasons, this shouldn’t be expected because it's a hold over from a request->response world that the SDK no longer uses."
        /// More on this here: https://github.com/Azure/azure-spatial-anchors-samples/issues/100
        /// </summary>
        private void OnAnchorLocated(object sender, AnchorLocatedEventArgs args)
        {
            Log.Info("The Cloud Session located anchors.");
            switch (args.Status)
            {
                case LocateAnchorStatus.Located:
                    // The anchors was located at the current physical space
                    CloudSpatialAnchor foundAnchor = args.Anchor;
                    if (foundAnchor != null && foundAnchor.LocalAnchor != null)
                    {
                        if (World.FromPerceptionAnchor(foundAnchor.LocalAnchor, out Pose at))
                        {
                            _locatedAnchor = foundAnchor;
                            _helmetPose = at;
                        }
                    }
                    break;
                case LocateAnchorStatus.AlreadyTracked:
                    Log.Info($"Anchor {args.Identifier} already tracked");
                    break;
                case LocateAnchorStatus.NotLocatedAnchorDoesNotExist:
                    Log.Info($"Anchor {args.Identifier} was deleted or never existed in the first place");
                    // The anchor was deleted or never existed in the first place
                    // Drop it, or show UI to ask user to anchor the content anew
                    break;
                case LocateAnchorStatus.NotLocated:
                    Log.Info($"Anchor {args.Identifier} hasn't been found given the location data");
                    // The anchor hasn't been found given the location data
                    // The user might in the wrong location, or maybe more data will help
                    // Show UI to tell user to keep looking around
                    break;
            }
            SetFeedback("");
            _sessionState = ASASessionState.IDLE;
        }

        /// <summary>
        /// Ops, it seems that we have a issue :/
        /// </summary>
        private void OnCloudSessionError(object sender, SessionErrorEventArgs args)
        {
            Log.Err("Ops, we had an error on the Cloud Session:" + args.ErrorMessage);
        }

        #endregion

        internal void Update()
        {
            RenderASAMenu();
            RenderHelmet();
        }

        private void RenderHelmet()
        {
            UIMove moveType = _locatedAnchor == null ? UIMove.Exact : UIMove.None;
            UI.Handle("helmet", ref _helmetPose, _helmetModel.Bounds * 0.1f, moveType: moveType);
            _helmetModel.Draw(_helmetPose.ToMatrix(0.1f));
        }

        private void RenderASAMenu()
        {
            UI.WindowBegin("ASA Menu", ref _asaMenuPose, new Vec2(25 * U.cm, 5f * U.cm), moveType: UIMove.Exact);
            UI.Text("Move the helmet around and use the buttons below to save it position to a Azure Cloud Anchor");
            UI.HSeparator();
            if (_sessionState == ASASessionState.NOT_STARTED)
            {
                if(UI.Button("Start Cloud Session"))
                    StartCloudSession();
            }
            else
            {
                if (_locatedAnchor == null && UI.Button("Save Anchor"))
                {
                    CreateCloudAnchor(_helmetPose);
                }

                if (_sessionState == ASASessionState.IDLE && _locatedAnchor != null && UI.Button("Delete Anchor"))
                {
                    DeleteCloudAnchor(_locatedAnchor);
                }
            }
            if (!string.IsNullOrEmpty(_feedbackMessage))
            {
                UI.HSeparator();
                UI.Text(_feedbackMessage);
            }
            UI.WindowEnd();

        }

        private void StartCloudSession()
        {
            _cloudSession = new CloudSpatialAnchorSession();
            /*
             * To use ASA you need an Azure Spatial Anchor account. To create one and get the details for the AccountId, AccountKey and AccountDomain below, please follow this guide:
             * https://docs.microsoft.com/en-us/azure/spatial-anchors/how-tos/create-asa-account?tabs=azure-portal 
             * 
             * The Configuration class is not included to protect the ASA keys :)
             */
            _cloudSession.Configuration.AccountId = Configuration.ASA_AccountId;
            _cloudSession.Configuration.AccountKey = Configuration.ASA_AccountKey;
            _cloudSession.Configuration.AccountDomain = Configuration.ASA_AccountDomain;
            /*
             * Lets just handle the basic cloud session events
             */
            _cloudSession.Error += OnCloudSessionError;
            _cloudSession.AnchorLocated += OnAnchorLocated;
            _cloudSession.LocateAnchorsCompleted += OnLocateAnchorsCompleted;
            _cloudSession.SessionUpdated += OnCloudSessionUpdated;

            /*
             * Lets improve anchors detection by enabling sensors. 
             */
            var sensorProvider = new PlatformLocationProvider();
            sensorProvider.Sensors.GeoLocationEnabled = true;
            sensorProvider.Sensors.WifiEnabled = true;
            _cloudSession.LocationProvider = sensorProvider;


            /*
             * Lets start our session
             */
            _cloudSession.Start();


            _anchorLocateCriteria = new AnchorLocateCriteria();
            _anchorLocateCriteria.Strategy = LocateStrategy.AnyStrategy;

            /*
             * Do we have persisted anchors, if yes, load them
             */
            //LoadCloudAnchorsFromStorage();
            LoadNearbyCloudAnchors();


            _cloudSession.CreateWatcher(_anchorLocateCriteria);
        }

        internal void Shutdown()
        {
            _cloudSession.Stop();
        }

        #region helper methods
        /// <summary>
        /// Show a feedback message to the user.
        /// </summary>
        /// <param name="message">The <see cref="String"/> message to show to the user.</param>
        void SetFeedback(string message)
        {
            _feedbackMessage = message;
        }
        #endregion

        #region ASA helpers



        /// <summary>
        /// We need a way to locate anchors and to load them to our cloud session. 
        /// This is an example of loading anchors near the device.
        /// For other location strategies please take a look at <see cref="https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.spatialanchors.anchorlocatecriteria?view=spatialanchors-dotnet"/>.
        /// </summary>
        private void LoadNearbyCloudAnchors()
        {
            NearDeviceCriteria nearDeviceCriteria = new NearDeviceCriteria();
            nearDeviceCriteria.DistanceInMeters = 50;
            nearDeviceCriteria.MaxResultCount = 10;
            _anchorLocateCriteria.NearDevice = nearDeviceCriteria;
        }

        /// <summary>
        /// We need a way to locate anchors and to load them to our cloud session. 
        /// This is an example of loading anchors from a storage, in this case a local file.
        /// For other location strategies please take a look at <see cref="https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.spatialanchors.anchorlocatecriteria?view=spatialanchors-dotnet"/>.
        /// </summary>
        private void LoadCloudAnchorsFromStorage()
        {
            _sessionState = ASASessionState.LOADING_ANCHORS;
            SetFeedback($"Trying to locate anchors near the device...");

            Platform.FilePicker(PickerMode.Open, file =>
            {
                string fileContent;
                if (Platform.ReadFile(file, out fileContent))
                {
                    if (!String.IsNullOrEmpty(fileContent))
                    {
                        SetFeedback("Found persisted anchors identifiers. Trying to now locate anchors in the physical space...");
                        /*
                         * If we got content from the file, than we have an anchor id we can try to locate on the physical space
                         */
                        _anchorLocateCriteria.Identifiers = new string[] { fileContent };
                    }
                    else
                    {
                        SetFeedback("No valid content found on the file.");
                    }
                }
                else
                {
                    _sessionState = ASASessionState.IDLE;
                    // no file found!
                }
            }, () =>
            {
                SetFeedback("No valid content found on the file.");
            }, ".txt");
        }

        /// <summary>
        /// Stores the pose of the helmet in a Azure Cloud Anchor and caches the ASA identifier to a local file
        /// </summary>
        /// <param name="pose"></param>
        async Task CreateCloudAnchor(Pose pose)
        {
            _sessionState = ASASessionState.SAVING_ANCHOR;
            SetFeedback("Saving anchor to cloud, please wait...");
            //Gets the SpatialLocator instance that tracks the location of the current device, such as a HoloLens, relative to the user's surroundings.
            SpatialLocator locator = SpatialLocator.GetDefault();
            //Creates a frame of reference that remains stationary relative to the user's surroundings, with its initial origin at the SpatialLocator's current location.
            SpatialStationaryFrameOfReference stationary = locator.CreateStationaryFrameOfReferenceAtCurrentLocation();
            // Creates an anchor at the origin of the specified coordinate system.
            SpatialAnchor anchor = SpatialAnchor.TryCreateRelativeTo(stationary.CoordinateSystem);
            // Converts a Windows.Perception.Spatial.SpatialAnchor’s pose into SteroKit’s coordinate system
            Pose anchorPose = World.FromPerceptionAnchor(anchor);

            Pose newAnchor = anchorPose.ToMatrix().Inverse.Transform(pose);
            anchor = SpatialAnchor.TryCreateRelativeTo(stationary.CoordinateSystem, newAnchor.position, newAnchor.orientation);
            pose = World.FromPerceptionAnchor(anchor);

            var cloudAnchor = new CloudSpatialAnchor();
            cloudAnchor.LocalAnchor = anchor;

            try
            {
                await _cloudSession.CreateAnchorAsync(cloudAnchor);
                _locatedAnchor = cloudAnchor;
                _sessionState = ASASessionState.IDLE;
                SetFeedback($"Cloud Anchor saved with success. ID: {cloudAnchor.Identifier}");

                // Example if you want to store the anchors IDs on a local file
                //Platform.FilePicker(PickerMode.Save, file =>
                //{
                //    if (Platform.WriteFile(file, cloudAnchor.Identifier))
                //    {
                //        _locatedAnchor = cloudAnchor;
                //        _sessionState = ASASessionState.IDLE;
                //        SetFeedback($"Cloud Anchor saved with success. ID: {cloudAnchor.Identifier}");
                //    }
                //    else
                //    {
                //        _locatedAnchor = null;
                //        _sessionState = ASASessionState.IDLE;
                //        SetFeedback("Ops. Could not save cloud anchor to local storage...");
                //    }
                //}, null, ".txt");

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// Deletes a cloud anchor
        /// </summary>
        /// <param name="anchor"></param>
        async Task DeleteCloudAnchor(CloudSpatialAnchor anchor)
        {
            _sessionState = ASASessionState.DELETING_ANCHOR;
            SetFeedback("Removing anchor, please wait...");
            if (_locatedAnchor != null)
            {
                await _cloudSession.DeleteAnchorAsync(anchor);
                _locatedAnchor = null;
            }
            _sessionState = ASASessionState.IDLE;
            SetFeedback(String.Empty);
        }
        #endregion
    }
}
