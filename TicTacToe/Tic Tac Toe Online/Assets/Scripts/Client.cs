using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.UI;

public class Client : MonoBehaviour
{

    [SerializeField] private GameObject _cross = null;
    [SerializeField] private GameObject _zero = null;
    [SerializeField] private LineRenderer _line = null;
    [Space]
    [SerializeField] private GameObject _lockPanel = null;
    [SerializeField] private Text _lockPanelText = null;
    [SerializeField] private Button _okButton = null;
    [SerializeField] private Button _cancelButton = null;
    [Space]
    [SerializeField] private Text _bottomInfoText = null;
    [SerializeField] private Text _yourPointsText = null;
    [SerializeField] private Text _opponentPointsText = null;
    [Space]
    [SerializeField] private Transform _leftTopButton = null;
    [SerializeField] private Transform _topButton = null;
    [SerializeField] private Transform _rightTopButton = null;
    [SerializeField] private Transform _leftButton = null;
    [SerializeField] private Transform _centerButton = null;
    [SerializeField] private Transform _rightButton = null;
    [SerializeField] private Transform _leftBottomButton = null;
    [SerializeField] private Transform _bottomButton = null;
    [SerializeField] private Transform _rightBottomButton = null;

    private ClientEvents _clientEvent = ClientEvents.NOP;
    private PlayerActions _playerAction = PlayerActions.NOP;
    private PlayerActionsRequests _playerActionRequest = PlayerActionsRequests.NOP;
    private bool _isYourTurn = false;

    private GameObject _yourSymbol = null;
    private GameObject _opponentSymbol = null;
    private List<GameObject> _symbols = new List<GameObject>();

    private float _symbolsForwardOffset = 0.8f;
    private float _lineForwardOffset = 0.5f;
    private float _lineExtraLength = 1f;
    Vector3[] positions = new Vector3[2];
    Vector3 direction;

    private byte[] _winBuffer = new byte[3];
    private byte[] _loseBuffer = new byte[3];
    private byte _lastPressedButtonNumber = 0;

    private uint[] _points = new uint[2];

    private Socket _socket;

    private void Start()
    {
        ConnectToServer(IPAddress.Loopback, 45555);
    }

    private void FixedUpdate()
    {

        switch (_clientEvent)
        {
            case ClientEvents.NOP:
                break;

            case ClientEvents.Disconnected:
                _lockPanel.SetActive(true);
                _lockPanelText.text = "Соединение разорвано.";
                _cancelButton.gameObject.SetActive(false);
                _okButton.gameObject.SetActive(true);
                _clientEvent = ClientEvents.NOP;
                break;

            case ClientEvents.StartGame:
                _lockPanel.SetActive(false);
                _cancelButton.gameObject.SetActive(false);
                _clientEvent = ClientEvents.NOP;
                if (_isYourTurn)
                {
                    _yourSymbol = _cross;
                    _opponentSymbol = _zero;
                    _bottomInfoText.text = "Ваш ход";
                }
                else
                {
                    _yourSymbol = _zero;
                    _opponentSymbol = _cross;
                    _bottomInfoText.text = "Ход противника";
                }
                break;

            case ClientEvents.NextGame:
                ClearGameGrid();
                _clientEvent = ClientEvents.NOP;
                if (_isYourTurn)
                {
                    _yourSymbol = _cross;
                    _opponentSymbol = _zero;
                    _bottomInfoText.text = "Ваш ход";
                }
                else
                {
                    _yourSymbol = _zero;
                    _opponentSymbol = _cross;
                    _bottomInfoText.text = "Ход противника";
                }
                break;

            case ClientEvents.OpponentLeftTheGame:
                _lockPanel.SetActive(true);
                _lockPanelText.text = "Оппонент покинул игру.";
                _okButton.gameObject.SetActive(true);
                _clientEvent = ClientEvents.NOP;
                break;
        }

        switch (_playerAction)
        {
            case PlayerActions.NOP:
                break;

            case PlayerActions.PressLeftTop:
                if (_isYourTurn)
                {
                    _symbols.Add(Instantiate(_yourSymbol, _leftTopButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = false;
                    _bottomInfoText.text = "Ход противника";
                }
                else
                {
                    _symbols.Add(Instantiate(_opponentSymbol, _leftTopButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = true;
                    _bottomInfoText.text = "Ваш ход";
                }
                _playerAction = PlayerActions.NOP;
                break;

            case PlayerActions.PressTop:
                if (_isYourTurn)
                {
                    _symbols.Add(Instantiate(_yourSymbol, _topButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = false;
                    _bottomInfoText.text = "Ход противника";
                }
                else
                {
                    _symbols.Add(Instantiate(_opponentSymbol, _topButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = true;
                    _bottomInfoText.text = "Ваш ход";
                }
                _playerAction = PlayerActions.NOP;
                break;

            case PlayerActions.PressRightTop:
                if (_isYourTurn)
                {
                    _symbols.Add(Instantiate(_yourSymbol, _rightTopButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = false;
                    _bottomInfoText.text = "Ход противника";
                }
                else
                {
                    _symbols.Add(Instantiate(_opponentSymbol, _rightTopButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = true;
                    _bottomInfoText.text = "Ваш ход";
                }
                _playerAction = PlayerActions.NOP;
                break;

            case PlayerActions.PressLeft:
                if (_isYourTurn)
                {
                    _symbols.Add(Instantiate(_yourSymbol, _leftButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = false;
                    _bottomInfoText.text = "Ход противника";
                }
                else
                {
                    _symbols.Add(Instantiate(_opponentSymbol, _leftButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = true;
                    _bottomInfoText.text = "Ваш ход";
                }
                _playerAction = PlayerActions.NOP;
                break;

            case PlayerActions.PressCenter:
                if (_isYourTurn)
                {
                    _symbols.Add(Instantiate(_yourSymbol, _centerButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = false;
                    _bottomInfoText.text = "Ход противника";
                }
                else
                {
                    _symbols.Add(Instantiate(_opponentSymbol, _centerButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = true;
                    _bottomInfoText.text = "Ваш ход";
                }
                _playerAction = PlayerActions.NOP;
                break;

            case PlayerActions.PressRight:
                if (_isYourTurn)
                {
                    _symbols.Add(Instantiate(_yourSymbol, _rightButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = false;
                    _bottomInfoText.text = "Ход противника";
                }
                else
                {
                    _symbols.Add(Instantiate(_opponentSymbol, _rightButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = true;
                    _bottomInfoText.text = "Ваш ход";
                }
                _playerAction = PlayerActions.NOP;
                break;

            case PlayerActions.PressLeftBottom:
                if (_isYourTurn)
                {
                    _symbols.Add(Instantiate(_yourSymbol, _leftBottomButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = false;
                    _bottomInfoText.text = "Ход противника";
                }
                else
                {
                    _symbols.Add(Instantiate(_opponentSymbol, _leftBottomButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = true;
                    _bottomInfoText.text = "Ваш ход";
                }
                _playerAction = PlayerActions.NOP;
                break;

            case PlayerActions.PressBottom:
                if (_isYourTurn)
                {
                    _symbols.Add(Instantiate(_yourSymbol, _bottomButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = false;
                    _bottomInfoText.text = "Ход противника";
                }
                else
                {
                    _symbols.Add(Instantiate(_opponentSymbol, _bottomButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = true;
                    _bottomInfoText.text = "Ваш ход";
                }
                _playerAction = PlayerActions.NOP;
                break;

            case PlayerActions.PressRightBottom:
                if (_isYourTurn)
                {
                    _symbols.Add(Instantiate(_yourSymbol, _rightBottomButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = false;
                    _bottomInfoText.text = "Ход противника";
                }
                else
                {
                    _symbols.Add(Instantiate(_opponentSymbol, _rightBottomButton.position + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = true;
                    _bottomInfoText.text = "Ваш ход";
                }
                _playerAction = PlayerActions.NOP;
                break;

            case PlayerActions.Win:
                _symbols.Add(Instantiate(_yourSymbol, GetButtonPositionWithNextNumber(_winBuffer[0]) + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                positions[0] = GetButtonPositionWithNextNumber(_winBuffer[1]);
                positions[1] = GetButtonPositionWithNextNumber(_winBuffer[2]);
                direction = (positions[1] - positions[0]).normalized;
                positions[0] -= direction * _lineExtraLength + Vector3.forward * _lineForwardOffset;
                positions[1] += direction * _lineExtraLength + Vector3.forward * _lineForwardOffset;
                _line.SetPositions(positions);
                _points[0]++;
                _yourPointsText.text = _points[0].ToString();
                _bottomInfoText.text = "Вы победили!";
                _playerAction = PlayerActions.NOP;
                break;

            case PlayerActions.Lose:
                _symbols.Add(Instantiate(_opponentSymbol, GetButtonPositionWithNextNumber(_loseBuffer[0]) + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                positions[0] = GetButtonPositionWithNextNumber(_loseBuffer[1]);
                positions[1] = GetButtonPositionWithNextNumber(_loseBuffer[2]);
                direction = (positions[1] - positions[0]).normalized;
                positions[0] -= direction * _lineExtraLength + Vector3.forward * _lineForwardOffset;
                positions[1] += direction * _lineExtraLength + Vector3.forward * _lineForwardOffset;
                _line.SetPositions(positions);
                _points[1]++;
                _opponentPointsText.text = _points[1].ToString();
                _bottomInfoText.text = "Вы проиграли!";
                _playerAction = PlayerActions.NOP;
                break;

            case PlayerActions.Draw:
                if (_isYourTurn)
                {
                    _symbols.Add(Instantiate(_yourSymbol, GetButtonPositionWithNextNumber(_lastPressedButtonNumber) + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = false;
                }
                else
                {
                    _symbols.Add(Instantiate(_opponentSymbol, GetButtonPositionWithNextNumber(_lastPressedButtonNumber) + Vector3.forward * _symbolsForwardOffset, Quaternion.identity));
                    _isYourTurn = true;
                }
                _bottomInfoText.text = "Ничья!";
                _playerAction = PlayerActions.NOP;
                break;
        }

    }

    private Vector3 GetButtonPositionWithNextNumber(byte num)
    {
        switch (num)
        {
            case 1: return _leftTopButton.position;
            case 2: return _topButton.position;
            case 3: return _rightTopButton.position;
            case 4: return _leftButton.position;
            case 5: return _centerButton.position;
            case 6: return _rightButton.position;
            case 7: return _leftBottomButton.position;
            case 8: return _bottomButton.position;
            case 9: return _rightBottomButton.position;
            default: return _leftTopButton.position;
        }
    }

    private void ClearGameGrid()
    {
        foreach (var item in _symbols)
        {
            Destroy(item);
        }

        _symbols.RemoveRange(0, _symbols.Count);
        _line.SetPosition(0, Vector3.zero);
        _line.SetPosition(1, Vector3.zero);
    }

    #region PressedButtons

    public void PressedLeftTopButton()
    {
        if (_isYourTurn) _playerActionRequest = PlayerActionsRequests.PressLeftTop;
    }

    public void PressedTopButton()
    {
        if (_isYourTurn) _playerActionRequest = PlayerActionsRequests.PressTop;
    }

    public void PressedRightTopButton()
    {
        if (_isYourTurn) _playerActionRequest = PlayerActionsRequests.PressRightTop;
    }

    public void PressedLeftButton()
    {
        if (_isYourTurn) _playerActionRequest = PlayerActionsRequests.PressLeft;
    }

    public void PressedCenterButton()
    {
        if (_isYourTurn) _playerActionRequest = PlayerActionsRequests.PressCenter;
    }

    public void PressedRightButton()
    {
        if (_isYourTurn) _playerActionRequest = PlayerActionsRequests.PressRight;
    }

    public void PressedLeftBottomButton()
    {
        if (_isYourTurn) _playerActionRequest = PlayerActionsRequests.PressLeftBottom;
    }

    public void PressedBottomButton()
    {
        if (_isYourTurn) _playerActionRequest = PlayerActionsRequests.PressBottom;
    }

    public void PressedRightBottomButton()
    {
        if (_isYourTurn) _playerActionRequest = PlayerActionsRequests.PressRightBottom;
    }

    public void PressedOkButton()
    {
        _socket.Close();
        SceneManager.LoadScene(0);
    }

    public void PressedCancelButton()
    {
        _socket.Close();
        SceneManager.LoadScene(0);
    }

    public void LoadMainMenu()
    {
        _playerActionRequest = PlayerActionsRequests.LeftTheGame;
        SceneManager.LoadScene(0);
    }

    #endregion

    private void ConnectToServer(IPAddress serverIP, uint port)
    {

        IPEndPoint endPoint = new IPEndPoint(serverIP, (int)port);
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            _socket.Connect(endPoint);
        }
        catch (System.Exception)
        {

            _lockPanelText.text = "Ошибка подключения к серверу.";
            _okButton.gameObject.SetActive(true);
            return;
        }

        _lockPanelText.text = "Поиск соперника...";
        _cancelButton.gameObject.SetActive(true);

        Thread thread = new Thread(ServerHandler);
        thread.IsBackground = true;
        thread.Start();
    }

    private void ServerHandler()
    {

        try
        {
            _socket.Receive(new byte[1]);
            _socket.Receive(new byte[1]);
        }
        catch (System.Exception)
        {

            _clientEvent = ClientEvents.Disconnected;
            return;
        }

        byte[] turnNum = new byte[1];

        try
        {
            _socket.Receive(turnNum);
        }
        catch (System.Exception)
        {

            _clientEvent = ClientEvents.Disconnected;
            return;
        }

        _isYourTurn = turnNum[0] == 1;
        _clientEvent = ClientEvents.StartGame;

        byte[] buffer = new byte[4];

        while (true)
        {
            try
            {
                switch (_playerActionRequest)
                {
                    case PlayerActionsRequests.NOP:
                        buffer[0] = 0;
                        _socket.Send(buffer);
                        break;
                    case PlayerActionsRequests.PressLeftTop:
                        buffer[0] = 1;
                        _socket.Send(buffer);
                        _playerActionRequest = PlayerActionsRequests.NOP;
                        break;
                    case PlayerActionsRequests.PressTop:
                        buffer[0] = 2;
                        _socket.Send(buffer);
                        _playerActionRequest = PlayerActionsRequests.NOP;
                        break;
                    case PlayerActionsRequests.PressRightTop:
                        buffer[0] = 3;
                        _socket.Send(buffer);
                        _playerActionRequest = PlayerActionsRequests.NOP;
                        break;
                    case PlayerActionsRequests.PressLeft:
                        buffer[0] = 4;
                        _socket.Send(buffer);
                        _playerActionRequest = PlayerActionsRequests.NOP;
                        break;
                    case PlayerActionsRequests.PressCenter:
                        buffer[0] = 5;
                        _socket.Send(buffer);
                        _playerActionRequest = PlayerActionsRequests.NOP;
                        break;
                    case PlayerActionsRequests.PressRight:
                        buffer[0] = 6;
                        _socket.Send(buffer);
                        _playerActionRequest = PlayerActionsRequests.NOP;
                        break;
                    case PlayerActionsRequests.PressLeftBottom:
                        buffer[0] = 7;
                        _socket.Send(buffer);
                        _playerActionRequest = PlayerActionsRequests.NOP;
                        break;
                    case PlayerActionsRequests.PressBottom:
                        buffer[0] = 8;
                        _socket.Send(buffer);
                        _playerActionRequest = PlayerActionsRequests.NOP;
                        break;
                    case PlayerActionsRequests.PressRightBottom:
                        buffer[0] = 9;
                        _socket.Send(buffer);
                        _playerActionRequest = PlayerActionsRequests.NOP;
                        break;
                    case PlayerActionsRequests.LeftTheGame:
                        buffer[0] = 10;
                        _socket.Send(buffer);
                        return;
                }
            }
            catch (System.Exception)
            {

                _clientEvent = ClientEvents.Disconnected;
                return;
            }

            try
            {
                _socket.Receive(buffer);
            }
            catch (System.Exception)
            {

                _clientEvent = ClientEvents.Disconnected;
                return;
            }

            switch ((PlayerActions)buffer[0])
            {
                case PlayerActions.NOP:
                    break;
                case PlayerActions.PressLeftTop:
                    _playerAction = PlayerActions.PressLeftTop;
                    break;
                case PlayerActions.PressTop:
                    _playerAction = PlayerActions.PressTop;
                    break;
                case PlayerActions.PressRightTop:
                    _playerAction = PlayerActions.PressRightTop;
                    break;
                case PlayerActions.PressLeft:
                    _playerAction = PlayerActions.PressLeft;
                    break;
                case PlayerActions.PressCenter:
                    _playerAction = PlayerActions.PressCenter;
                    break;
                case PlayerActions.PressRight:
                    _playerAction = PlayerActions.PressRight;
                    break;
                case PlayerActions.PressLeftBottom:
                    _playerAction = PlayerActions.PressLeftBottom;
                    break;
                case PlayerActions.PressBottom:
                    _playerAction = PlayerActions.PressBottom;
                    break;
                case PlayerActions.PressRightBottom:
                    _playerAction = PlayerActions.PressRightBottom;
                    break;
                case PlayerActions.Win:
                    _winBuffer[0] = buffer[1];
                    _winBuffer[1] = buffer[2];
                    _winBuffer[2] = buffer[3];
                    _playerAction = PlayerActions.Win;
                    _socket.Receive(buffer);
                    _clientEvent = ClientEvents.NextGame;
                    _playerActionRequest = PlayerActionsRequests.NOP;
                    break;
                case PlayerActions.Lose:
                    _loseBuffer[0] = buffer[1];
                    _loseBuffer[1] = buffer[2];
                    _loseBuffer[2] = buffer[3];
                    _playerAction = PlayerActions.Lose;
                    _socket.Receive(buffer);
                    _clientEvent = ClientEvents.NextGame;
                    break;
                case PlayerActions.Draw:
                    _lastPressedButtonNumber = buffer[1];
                    _playerAction = PlayerActions.Draw;
                    _socket.Receive(buffer);
                    _clientEvent = ClientEvents.NextGame;
                    _playerActionRequest = PlayerActionsRequests.NOP;
                    break;
                case PlayerActions.LeftTheGame:
                    _clientEvent = ClientEvents.OpponentLeftTheGame;
                    _socket.Close();
                    return;
            }

            Thread.Sleep(100);
        }
    }

    #region Enumerations

    enum PlayerActions
    {
        NOP,
        PressLeftTop,
        PressTop,
        PressRightTop,
        PressLeft,
        PressCenter,
        PressRight,
        PressLeftBottom,
        PressBottom,
        PressRightBottom,
        Win,
        Lose,
        Draw,
        LeftTheGame
    }

    enum PlayerActionsRequests
    {
        NOP,
        PressLeftTop,
        PressTop,
        PressRightTop,
        PressLeft,
        PressCenter,
        PressRight,
        PressLeftBottom,
        PressBottom,
        PressRightBottom,
        LeftTheGame
    }

    enum ClientEvents
    {
        NOP,
        Disconnected,
        StartGame,
        NextGame,
        OpponentLeftTheGame
    }

    #endregion
}
