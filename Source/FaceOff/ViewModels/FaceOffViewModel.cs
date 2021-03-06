using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;
using FaceOff.Shared;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Plugin.Media.Abstractions;
using Xamarin.Forms;

namespace FaceOff
{
    public class FaceOffViewModel : BaseViewModel
    {
        readonly Lazy<string[]> _emotionStringsForAlertMessageHolder = new Lazy<string[]>(() =>
            new string[] { "angry", "disrespectful", "disgusted", "scared", "happy", "blank", "sad", "surprised" });

        const string _makeAFaceAlertMessage = "take a selfie looking";
        const string _calculatingScoreMessage = "Analyzing";

        const string _playerNumberNotImplentedExceptionText = "Player Number Not Implemented";

        readonly WeakEventManager<AlertMessageEventArgs> _popUpAlertAboutEmotionTriggeredEventManager = new WeakEventManager<AlertMessageEventArgs>();
        readonly WeakEventManager<string> _allEmotionResultsAlertTriggeredEventManager = new WeakEventManager<string>();
        readonly WeakEventManager _scoreButton1RevealTriggeredEventManager = new WeakEventManager();
        readonly WeakEventManager _scoreButton2RevealTriggeredEventManager = new WeakEventManager();
        readonly WeakEventManager _photoImage1RevealTriggeredEventManager = new WeakEventManager();
        readonly WeakEventManager _photoImage2RevealTriggeredEventManager = new WeakEventManager();
        readonly WeakEventManager _scoreButton1HideTriggeredEventManager = new WeakEventManager();
        readonly WeakEventManager _scoreButton2HidelTriggeredEventManager = new WeakEventManager();
        readonly WeakEventManager _photoImage1HideTriggeredEventManager = new WeakEventManager();
        readonly WeakEventManager _photoImage2HideTriggeredEventManager = new WeakEventManager();

        ImageSource? _photo1ImageSource, _photo2ImageSource;

        bool _isTakeLeftPhotoButtonEnabled = true;
        bool _isTakeLeftPhotoButtonStackVisible = true;
        bool _isTakeRightPhotoButtonEnabled = true;
        bool _isTakeRightPhotoButtonStackVisible = true;
        bool _isResetButtonEnabled;
        bool _isCalculatingPhoto1Score, _isCalculatingPhoto2Score;
        bool _isScore1ButtonEnabled, _isScore2ButtonEnabled;
        EmotionType _currentEmotionType;

        string _photo1Results = string.Empty,
            _photo2Results = string.Empty,
            _pageTitle = string.Empty,
            _scoreButton1Text = string.Empty,
            _scoreButton2Text = string.Empty;

        ICommand? _resetButtonPressed, _emotionPopUpAlertResponseCommand, _takePhoto1ButtonPressed,
            _takePhoto2ButtonPressed, _photo1ScoreButtonPressed, _photo2ScoreButtonPressed;

        public FaceOffViewModel()
        {
            IsResetButtonEnabled = false;

            SetRandomEmotion();
        }

        public event EventHandler<AlertMessageEventArgs> PopUpAlertAboutEmotionTriggered
        {
            add => _popUpAlertAboutEmotionTriggeredEventManager.AddEventHandler(value);
            remove => _popUpAlertAboutEmotionTriggeredEventManager.RemoveEventHandler(value);
        }

        public event EventHandler<string> AllEmotionResultsAlertTriggered
        {
            add => _allEmotionResultsAlertTriggeredEventManager.AddEventHandler(value);
            remove => _allEmotionResultsAlertTriggeredEventManager.RemoveEventHandler(value);
        }

        public event EventHandler ScoreButton1RevealTriggered
        {
            add => _scoreButton1RevealTriggeredEventManager.AddEventHandler(value);
            remove => _scoreButton1RevealTriggeredEventManager.RemoveEventHandler(value);
        }

        public event EventHandler ScoreButton2RevealTriggered
        {
            add => _scoreButton2RevealTriggeredEventManager.AddEventHandler(value);
            remove => _scoreButton2RevealTriggeredEventManager.RemoveEventHandler(value);
        }

        public event EventHandler PhotoImage1RevealTriggered
        {
            add => _photoImage1RevealTriggeredEventManager.AddEventHandler(value);
            remove => _photoImage1RevealTriggeredEventManager.RemoveEventHandler(value);
        }

        public event EventHandler PhotoImage2RevealTriggered
        {
            add => _photoImage2RevealTriggeredEventManager.AddEventHandler(value);
            remove => _photoImage2RevealTriggeredEventManager.RemoveEventHandler(value);
        }

        public event EventHandler ScoreButton1HideTriggered
        {
            add => _scoreButton1HideTriggeredEventManager.AddEventHandler(value);
            remove => _scoreButton1HideTriggeredEventManager.RemoveEventHandler(value);
        }

        public event EventHandler ScoreButton2HideTriggered
        {
            add => _scoreButton2HidelTriggeredEventManager.AddEventHandler(value);
            remove => _scoreButton2HidelTriggeredEventManager.RemoveEventHandler(value);
        }

        public event EventHandler PhotoImage1HideTriggered
        {
            add => _photoImage1HideTriggeredEventManager.AddEventHandler(value);
            remove => _photoImage1HideTriggeredEventManager.RemoveEventHandler(value);
        }

        public event EventHandler PhotoImage2HideTriggered
        {
            add => _photoImage2HideTriggeredEventManager.AddEventHandler(value);
            remove => _photoImage2HideTriggeredEventManager.RemoveEventHandler(value);
        }

        public ICommand EmotionPopUpAlertResponseCommand => _emotionPopUpAlertResponseCommand ??= new AsyncCommand<EmotionPopupResponseModel>(ExecuteEmotionPopUpAlertResponseCommand);
        public ICommand TakePhoto1ButtonPressed => _takePhoto1ButtonPressed ??= new Command(ExecuteTakePhoto1ButtonPressed);
        public ICommand TakePhoto2ButtonPressed => _takePhoto2ButtonPressed ??= new Command(ExecuteTakePhoto2ButtonPressed);
        public ICommand ResetButtonPressed => _resetButtonPressed ??= new Command(ExecuteResetButtonPressed);
        public ICommand Photo1ScoreButtonPressed => _photo1ScoreButtonPressed ??= new Command(ExecutePhoto1ScoreButtonPressed);
        public ICommand Photo2ScoreButtonPressed => _photo2ScoreButtonPressed ??= new Command(ExecutePhoto2ScoreButtonPressed);

        public ImageSource? Photo1ImageSource
        {
            get => _photo1ImageSource;
            set => SetProperty(ref _photo1ImageSource, value);
        }

        public ImageSource? Photo2ImageSource
        {
            get => _photo2ImageSource;
            set => SetProperty(ref _photo2ImageSource, value);
        }

        public bool IsTakeLeftPhotoButtonEnabled
        {
            get => _isTakeLeftPhotoButtonEnabled;
            set => SetProperty(ref _isTakeLeftPhotoButtonEnabled, value);
        }

        public bool IsTakeLeftPhotoButtonStackVisible
        {
            get => _isTakeLeftPhotoButtonStackVisible;
            set => SetProperty(ref _isTakeLeftPhotoButtonStackVisible, value);
        }

        public bool IsTakeRightPhotoButtonEnabled
        {
            get => _isTakeRightPhotoButtonEnabled;
            set => SetProperty(ref _isTakeRightPhotoButtonEnabled, value);
        }

        public bool IsTakeRightPhotoButtonStackVisible
        {
            get => _isTakeRightPhotoButtonStackVisible;
            set => SetProperty(ref _isTakeRightPhotoButtonStackVisible, value);
        }

        public string PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        public string ScoreButton1Text
        {
            get => _scoreButton1Text;
            set => SetProperty(ref _scoreButton1Text, value);
        }

        public string ScoreButton2Text
        {
            get => _scoreButton2Text;
            set => SetProperty(ref _scoreButton2Text, value);
        }

        public bool IsCalculatingPhoto1Score
        {
            get => _isCalculatingPhoto1Score;
            set => SetProperty(ref _isCalculatingPhoto1Score, value);
        }

        public bool IsCalculatingPhoto2Score
        {
            get => _isCalculatingPhoto2Score;
            set => SetProperty(ref _isCalculatingPhoto2Score, value);
        }

        public bool IsResetButtonEnabled
        {
            get => _isResetButtonEnabled;
            set => SetProperty(ref _isResetButtonEnabled, value);
        }

        public bool IsScore1ButtonEnabled
        {
            get => _isScore1ButtonEnabled;
            set => SetProperty(ref _isScore1ButtonEnabled, value);
        }

        public bool IsScore2ButtonEnabled
        {
            get => _isScore2ButtonEnabled;
            set => SetProperty(ref _isScore2ButtonEnabled, value);
        }

        string[] EmotionStringsForAlertMessage => _emotionStringsForAlertMessageHolder.Value;

        #region UITest Backdoor Methods
#if DEBUG
        public Task SubmitPhoto(EmotionType emotion, PlayerModel player)
        {
            _currentEmotionType = emotion;
            SetPageTitle(_currentEmotionType);

            return ExecuteGetPhotoResultsWorkflow(player);
        }
#endif
        #endregion
        void ExecuteTakePhoto1ButtonPressed() =>
            ExecutePopUpAlert(new PlayerModel(PlayerNumberType.Player1, PreferencesService.Player1Name));

        void ExecuteTakePhoto2ButtonPressed() =>
            ExecutePopUpAlert(new PlayerModel(PlayerNumberType.Player2, PreferencesService.Player2Name));

        void ExecutePopUpAlert(PlayerModel playerModel)
        {
            LogPhotoButtonTapped(playerModel.PlayerNumber);
            DisableButtons(playerModel.PlayerNumber);

            var title = EmotionConstants.EmotionDictionary[_currentEmotionType];
            var message = $"{playerModel.PlayerName}, {_makeAFaceAlertMessage} {EmotionStringsForAlertMessage[(int)_currentEmotionType]}";
            OnPopUpAlertAboutEmotionTriggered(title, message, playerModel);
        }

        Task ExecuteEmotionPopUpAlertResponseCommand(EmotionPopupResponseModel response)
        {
            var player = response.Player;

            if (response.IsPopUpConfirmed)
                return ExecuteTakePhotoWorkflow(player);

            EnableButtons(player.PlayerNumber);
            return Task.CompletedTask;
        }

        async Task ExecuteTakePhotoWorkflow(PlayerModel player)
        {
            player.ImageMediaFile = await MediaService.GetMediaFileFromCamera("FaceOff", player.PlayerNumber).ConfigureAwait(false);

            if (player.ImageMediaFile is null)
                EnableButtons(player.PlayerNumber);
            else
                await ExecuteGetPhotoResultsWorkflow(player).ConfigureAwait(false);
        }

        async Task ExecuteGetPhotoResultsWorkflow(PlayerModel player)
        {
            AnalyticsService.Track(AnalyticsConstants.PhotoTaken);

            await ConfigureUIForPendingEmotionResults(player).ConfigureAwait(false);

            var results = await GenerateEmotionResults(player).ConfigureAwait(false);

            await ConfigureUIForFinalizedEmotionResults(player, results).ConfigureAwait(false);
        }

        async Task ConfigureUIForPendingEmotionResults(PlayerModel player)
        {
            RevealPhoto(player.PlayerNumber);

            SetIsEnabledForOppositePhotoButton(true, player.PlayerNumber);

            SetIsEnabledForCurrentPhotoButton(false, player.PlayerNumber);
            SetIsVisibleForCurrentPhotoStack(false, player.PlayerNumber);

            SetScoreButtonText(_calculatingScoreMessage, player.PlayerNumber);

            SetPhotoImageSource(player.ImageMediaFile, player.PlayerNumber);

            SetIsCalculatingPhotoScore(true, player.PlayerNumber);

            SetResetButtonIsEnabled();

            await WaitForAnimationsToFinish((int)Math.Ceiling(AnimationConstants.DefaultAnimationTime * 2.5)).ConfigureAwait(false);

            RevealPhotoButton(player.PlayerNumber);
        }

        Task ConfigureUIForFinalizedEmotionResults(PlayerModel player, string results)
        {
            SetPhotoResultsText(results, player.PlayerNumber);

            SetIsCalculatingPhotoScore(false, player.PlayerNumber);

            SetResetButtonIsEnabled();

            SetIsEnabledForCurrentPlayerScoreButton(true, player.PlayerNumber);

            return WaitForAnimationsToFinish((int)Math.Ceiling(AnimationConstants.DefaultAnimationTime * 2.5));
        }

        async Task<string> GenerateEmotionResults(PlayerModel player)
        {
            List<Emotion> emotionArray = Enumerable.Empty<Emotion>().ToList();
            string emotionScore;

            try
            {
                emotionArray = await EmotionService.GetEmotionResultsFromMediaFile(player.ImageMediaFile).ConfigureAwait(false);
                emotionScore = EmotionService.GetPhotoEmotionScore(emotionArray, 0, _currentEmotionType);
            }
            catch (APIErrorException e) when (e.Response.StatusCode is System.Net.HttpStatusCode.Unauthorized)
            {
                AnalyticsService.Report(e);

                emotionScore = EmotionService.ErrorMessageDictionary[ErrorMessageType.InvalidAPIKey];
            }
            catch (HttpRequestException e) when (e.Message.Contains("offline"))
            {
                AnalyticsService.Report(e);

                emotionScore = EmotionService.ErrorMessageDictionary[ErrorMessageType.DeviceOffline];
            }
            catch (Exception e)
            {
                AnalyticsService.Report(e);

                emotionScore = EmotionService.ErrorMessageDictionary[ErrorMessageType.ConnectionToCognitiveServicesFailed];
            }

            var doesEmotionScoreContainErrorMessage = EmotionService.DoesStringContainErrorMessage(emotionScore);

            if (doesEmotionScoreContainErrorMessage)
            {
                var errorMessageKey = EmotionService.ErrorMessageDictionary.First(x => x.Value.Contains(emotionScore)).Key;

                switch (errorMessageKey)
                {
                    case ErrorMessageType.NoFaceDetected:
                        AnalyticsService.Track(EmotionService.ErrorMessageDictionary[ErrorMessageType.NoFaceDetected]);
                        break;
                    case ErrorMessageType.MultipleFacesDetected:
                        AnalyticsService.Track(EmotionService.ErrorMessageDictionary[ErrorMessageType.MultipleFacesDetected]);
                        break;
                    case ErrorMessageType.GenericError:
                        AnalyticsService.Track(EmotionService.ErrorMessageDictionary[ErrorMessageType.MultipleFacesDetected]);
                        break;
                }

                SetScoreButtonText(emotionScore, player.PlayerNumber);
            }
            else
                SetScoreButtonText($"Score: {emotionScore}", player.PlayerNumber);

            return EmotionService.GetStringOfAllPhotoEmotionScores(emotionArray, 0);
        }

        void ExecuteResetButtonPressed()
        {
            AnalyticsService.Track(AnalyticsConstants.ResetButtonTapped);

            SetRandomEmotion();

            Photo1ImageSource = null;
            Photo2ImageSource = null;

            IsTakeLeftPhotoButtonEnabled = true;
            IsTakeLeftPhotoButtonStackVisible = true;

            IsTakeRightPhotoButtonEnabled = true;
            IsTakeRightPhotoButtonStackVisible = true;

            ScoreButton1Text = string.Empty;
            ScoreButton2Text = string.Empty;

            IsScore1ButtonEnabled = false;
            IsScore2ButtonEnabled = false;

            _photo1Results = string.Empty;
            _photo2Results = string.Empty;

            OnPhotoImage1HideTriggered();
            OnPhotoImage2HideTriggered();
            OnScoreButton1HideTriggered();
            OnScoreButton2HideTriggered();
        }

        void ExecutePhoto1ScoreButtonPressed()
        {
            AnalyticsService.Track(AnalyticsConstants.ResultsButton1Tapped);
            OnAllEmotionResultsAlertTriggered(_photo1Results);
        }

        void ExecutePhoto2ScoreButtonPressed()
        {
            AnalyticsService.Track(AnalyticsConstants.ResultsButton2Tapped);
            OnAllEmotionResultsAlertTriggered(_photo2Results);
        }

        void SetRandomEmotion() => SetEmotion(EmotionService.GetRandomEmotionType(_currentEmotionType));


        void SetEmotion(EmotionType emotionType)
        {
            _currentEmotionType = emotionType;

            SetPageTitle(_currentEmotionType);
        }

        void RevealPhoto(PlayerNumberType playerNumber)
        {
            switch (playerNumber)
            {
                case PlayerNumberType.Player1:
                    OnPhotoImage1RevealTriggered();
                    break;
                case PlayerNumberType.Player2:
                    OnPhotoImage2RevealTriggered();
                    break;
                default:
                    throw new NotSupportedException(_playerNumberNotImplentedExceptionText);
            }
        }

        void SetIsEnabledForOppositePhotoButton(bool isEnabled, PlayerNumberType playerNumber)
        {
            switch (playerNumber)
            {
                case PlayerNumberType.Player1:
                    IsTakeRightPhotoButtonEnabled = isEnabled;
                    break;
                case PlayerNumberType.Player2:
                    IsTakeLeftPhotoButtonEnabled = isEnabled;
                    break;
                default:
                    throw new NotSupportedException(_playerNumberNotImplentedExceptionText);
            }
        }

        void SetIsEnabledForCurrentPlayerScoreButton(bool isEnabled, PlayerNumberType playerNumber)
        {
            switch (playerNumber)
            {
                case PlayerNumberType.Player1:
                    IsScore1ButtonEnabled = isEnabled;
                    break;
                case PlayerNumberType.Player2:
                    IsScore2ButtonEnabled = isEnabled;
                    break;
                default:
                    throw new NotSupportedException(_playerNumberNotImplentedExceptionText);
            }
        }

        void SetIsEnabledForButtons(bool isEnabled, PlayerNumberType playerNumber)
        {
            SetIsEnabledForOppositePhotoButton(isEnabled, playerNumber);
            SetIsEnabledForCurrentPlayerScoreButton(isEnabled, playerNumber);
        }

        void SetIsEnabledForCurrentPhotoButton(bool isEnabled, PlayerNumberType playerNumber)
        {
            switch (playerNumber)
            {
                case PlayerNumberType.Player1:
                    IsTakeLeftPhotoButtonEnabled = isEnabled;
                    break;
                case PlayerNumberType.Player2:
                    IsTakeRightPhotoButtonEnabled = isEnabled;
                    break;
                default:
                    throw new NotSupportedException(_playerNumberNotImplentedExceptionText);
            }
        }

        void SetScoreButtonText(string scoreButtonText, PlayerNumberType playerNumber)
        {
            switch (playerNumber)
            {
                case PlayerNumberType.Player1:
                    ScoreButton1Text = scoreButtonText;
                    break;
                case PlayerNumberType.Player2:
                    ScoreButton2Text = scoreButtonText;
                    break;
                default:
                    throw new NotSupportedException(_playerNumberNotImplentedExceptionText);
            }
        }

        void SetIsVisibleForCurrentPhotoStack(bool isVisible, PlayerNumberType playerNumber)
        {
            switch (playerNumber)
            {
                case PlayerNumberType.Player1:
                    IsTakeLeftPhotoButtonStackVisible = isVisible;
                    break;
                case PlayerNumberType.Player2:
                    IsTakeRightPhotoButtonStackVisible = isVisible;
                    break;
                default:
                    throw new NotSupportedException(_playerNumberNotImplentedExceptionText);
            }
        }

        void SetPhotoImageSource(MediaFile? imageMediaFile, PlayerNumberType playerNumber)
        {
            switch (playerNumber)
            {
                case PlayerNumberType.Player1:
                    Photo1ImageSource = ImageSource.FromStream(() => imageMediaFile?.GetStream());
                    break;
                case PlayerNumberType.Player2:
                    Photo2ImageSource = ImageSource.FromStream(() => imageMediaFile?.GetStream());
                    break;
                default:
                    throw new NotSupportedException(_playerNumberNotImplentedExceptionText);
            }
        }

        void SetIsCalculatingPhotoScore(bool isCalculatingScore, PlayerNumberType playerNumber)
        {
            switch (playerNumber)
            {
                case PlayerNumberType.Player1:
                    IsCalculatingPhoto1Score = isCalculatingScore;
                    break;
                case PlayerNumberType.Player2:
                    IsCalculatingPhoto2Score = isCalculatingScore;
                    break;
                default:
                    throw new NotSupportedException(_playerNumberNotImplentedExceptionText);
            }
        }

        void RevealPhotoButton(PlayerNumberType playerNumber)
        {
            switch (playerNumber)
            {
                case PlayerNumberType.Player1:
                    OnScoreButton1RevealTriggered();
                    break;
                case PlayerNumberType.Player2:
                    OnScoreButton2RevealTriggered();
                    break;
                default:
                    throw new NotSupportedException(_playerNumberNotImplentedExceptionText);
            }
        }

        void SetPhotoResultsText(string results, PlayerNumberType playerNumber)
        {
            switch (playerNumber)
            {
                case PlayerNumberType.Player1:
                    _photo1Results = results;
                    break;
                case PlayerNumberType.Player2:
                    _photo2Results = results;
                    break;
                default:
                    throw new NotSupportedException(_playerNumberNotImplentedExceptionText);
            }
        }

        void LogPhotoButtonTapped(PlayerNumberType playerNumber)
        {
            switch (playerNumber)
            {
                case PlayerNumberType.Player1:
                    AnalyticsService.Track(AnalyticsConstants.PhotoButton1Tapped);
                    break;
                case PlayerNumberType.Player2:
                    AnalyticsService.Track(AnalyticsConstants.PhotoButton2Tapped);
                    break;
                default:
                    throw new NotSupportedException(_playerNumberNotImplentedExceptionText);
            }
        }

        void SetPageTitle(EmotionType emotionType) =>
            PageTitle = EmotionConstants.EmotionDictionary[emotionType];

        Task WaitForAnimationsToFinish(int waitTimeInSeconds) => Task.Delay(waitTimeInSeconds);

        void EnableButtons(PlayerNumberType playerNumber) =>
            SetIsEnabledForButtons(true, playerNumber);

        void DisableButtons(PlayerNumberType playerNumber) =>
            SetIsEnabledForButtons(false, playerNumber);

        void SetResetButtonIsEnabled() =>
            IsResetButtonEnabled = !(IsCalculatingPhoto1Score || IsCalculatingPhoto2Score);

        void OnAllEmotionResultsAlertTriggered(string emotionResults) =>
            _allEmotionResultsAlertTriggeredEventManager.HandleEvent(this, emotionResults, nameof(AllEmotionResultsAlertTriggered));

        void OnPhotoImage1RevealTriggered() =>
            _photoImage1RevealTriggeredEventManager.HandleEvent(this, EventArgs.Empty, nameof(PhotoImage1RevealTriggered));

        void OnScoreButton1RevealTriggered() =>
            _scoreButton1RevealTriggeredEventManager.HandleEvent(this, EventArgs.Empty, nameof(ScoreButton1RevealTriggered));

        void OnPhotoImage2RevealTriggered() =>
            _photoImage2RevealTriggeredEventManager.HandleEvent(this, EventArgs.Empty, nameof(PhotoImage2RevealTriggered));

        void OnScoreButton2RevealTriggered() =>
            _scoreButton2RevealTriggeredEventManager.HandleEvent(this, EventArgs.Empty, nameof(ScoreButton2RevealTriggered));

        void OnPhotoImage1HideTriggered() =>
            _photoImage1HideTriggeredEventManager.HandleEvent(this, EventArgs.Empty, nameof(PhotoImage1HideTriggered));

        void OnScoreButton1HideTriggered() =>
            _scoreButton1HideTriggeredEventManager.HandleEvent(this, EventArgs.Empty, nameof(ScoreButton1HideTriggered));

        void OnPhotoImage2HideTriggered() =>
            _photoImage2HideTriggeredEventManager.HandleEvent(this, EventArgs.Empty, nameof(PhotoImage2HideTriggered));

        void OnScoreButton2HideTriggered() =>
            _scoreButton2HidelTriggeredEventManager.HandleEvent(this, EventArgs.Empty, nameof(ScoreButton2HideTriggered));

        void OnPopUpAlertAboutEmotionTriggered(string title, string message, PlayerModel player) =>
            _popUpAlertAboutEmotionTriggeredEventManager.HandleEvent(this, new AlertMessageEventArgs(title, message, player), nameof(PopUpAlertAboutEmotionTriggered));
    }
}

