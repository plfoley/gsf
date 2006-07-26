'*******************************************************************************************************
'  Tva.Communication.ClientBase.vb - Base functionality of a client for transporting data
'  Copyright � 2006 - TVA, all rights reserved - Gbtc
'
'  Build Environment: VB.NET, Visual Studio 2005
'  Primary Developer: Pinal C. Patel, Operations Data Architecture [TVA]
'      Office: COO - TRNS/PWR ELEC SYS O, CHATTANOOGA, TN - MR 2W-C
'       Phone: 423/751-2250
'       Email: pcpatel@tva.gov
'
'  Code Modification History:
'  -----------------------------------------------------------------------------------------------------
'  06/01/2006 - Pinal C. Patel
'       Original version of source code generated
'
'*******************************************************************************************************

Imports System.Text
Imports System.Drawing
Imports System.ComponentModel
Imports Tva.Common
Imports Tva.Serialization
Imports Tva.DateTime.Common
Imports Tva.Communication.CommunicationHelper

''' <summary>
''' Represents a client involved in the transportation of data.
''' </summary>
<ToolboxBitmap(GetType(CommunicationClientBase)), DefaultEvent("ReceivedData")> _
Public MustInherit Class CommunicationClientBase
    Implements ICommunicationClient

    Private m_connectionString As String
    Private m_receiveBufferSize As Integer
    Private m_receiveTimeout As Integer
    Private m_maximumConnectionAttempts As Integer
    Private m_textEncoding As Encoding
    Private m_protocol As TransportProtocol
    Private m_secureSession As Boolean
    Private m_handshake As Boolean
    Private m_handshakePassphrase As String
    Private m_encryption As Tva.Security.Cryptography.EncryptLevel
    Private m_compression As Tva.IO.Compression.CompressLevel
    Private m_crcCheck As CRCCheckType
    Private m_enabled As Boolean
    Private m_serverID As Guid
    Private m_clientID As Guid
    Private m_isConnected As Boolean
    Private m_connectTime As Long
    Private m_disconnectTime As Long
    Private m_totalBytesSent As Integer
    Private m_totalBytesReceived As Integer

    ''' <summary>
    ''' The maximum number of bytes that can be sent from the client to server in a single send operation.
    ''' </summary>
    Public Const MaximumDataSize As Integer = 524288000  ' 500 MB

    ''' <summary>
    ''' Occurs when the client is trying to connect to the server.
    ''' </summary>
    <Description("Occurs when the client is trying to connect to the server."), Category("Connection")> _
    Public Event Connecting(ByVal sender As Object, ByVal e As System.EventArgs) Implements ICommunicationClient.Connecting

    ''' <summary>
    ''' Occurs when connecting of the client to the server has been cancelled.
    ''' </summary>
    <Description("Occurs when connecting of the client to the server has been cancelled."), Category("Connection")> _
    Public Event ConnectingCancelled(ByVal sender As Object, ByVal e As System.EventArgs) Implements ICommunicationClient.ConnectingCancelled

    ''' <summary>
    ''' Occurs when an exception is encountered while connecting to the server.
    ''' </summary>
    ''' <param name="ex">The exception that was encountered while connecting to the server.</param>
    <Description("Occurs when an exception occurs while connecting to the server."), Category("Connection")> _
    Public Event ConnectingException(ByVal ex As Exception) Implements ICommunicationClient.ConnectingException

    ''' <summary>
    ''' Occurs when the client has successfully connected to the server.
    ''' </summary>
    <Description("Occurs when the client has successfully connected to the server."), Category("Connection")> _
    Public Event Connected(ByVal sender As Object, ByVal e As System.EventArgs) Implements ICommunicationClient.Connected

    ''' <summary>
    ''' Occurs when the client has disconnected from the server.
    ''' </summary>
    <Description("Occurs when the client has disconnected from the server."), Category("Connection")> _
    Public Event Disconnected(ByVal sender As Object, ByVal e As System.EventArgs) Implements ICommunicationClient.Disconnected

    ''' <summary>
    ''' Occurs when the client begins sending data to the server.
    ''' </summary>
    ''' <param name="data">The data being sent to the server.</param>
    <Description("Occurs when the client begins sending data to the server."), Category("Data")> _
    Public Event SendDataBegin(ByVal data As Byte()) Implements ICommunicationClient.SendDataBegin

    ''' <summary>
    ''' Occurs when the client has successfully send data to the server.
    ''' </summary>
    ''' <param name="data">The data sent to the server.</param>
    <Description("Occurs when the client has successfully send data to the server."), Category("Data")> _
    Public Event SendDataComplete(ByVal data As Byte()) Implements ICommunicationClient.SendDataComplete

    ''' <summary>
    ''' Occurs when the client receives data from the server.
    ''' </summary>
    ''' <param name="data">The data that was received from the server.</param>
    <Description("Occurs when the client receives data from the server."), Category("Data")> _
    Public Event ReceivedData(ByVal data As Byte()) Implements ICommunicationClient.ReceivedData

    ''' <summary>
    ''' Occurs when no data is received from the server after waiting for the specified time.
    ''' </summary>
    <Description("Occurs when no data is received from the server after waiting for the specified time."), Category("Data")> _
    Public Event ReceiveTimedOut(ByVal sender As Object, ByVal e As System.EventArgs) Implements ICommunicationClient.ReceiveTimedOut

    ''' <summary>
    ''' Gets or sets the data required by the client to connect to the server.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The data required by the client to connect to the server.</returns>
    <Description("The data required by the client to connect to the server."), Category("Configuration")> _
    Public Property ConnectionString() As String Implements ICommunicationClient.ConnectionString
        Get
            Return m_connectionString
        End Get
        Set(ByVal value As String)
            If ValidConnectionString(value) Then
                m_connectionString = value
                If IsConnected() Then
                    ' Reconnect the client when connection data is changed.
                    Disconnect()
                    Connect()
                End If
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the maximum number of times the client will attempt to connect to the server.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The maximum number of times the client will attempt to connect to the server.</returns>
    ''' <remarks>Set MaximumConnectionAttempts = -1 for infinite connection attempts.</remarks>
    <Description("The maximum number of times the client will attempt to connect to the server. Set MaximumConnectionAttempts = -1 for infinite connection attempts."), Category("Configuration"), DefaultValue(GetType(Integer), "-1")> _
    Public Property MaximumConnectionAttempts() As Integer Implements ICommunicationClient.MaximumConnectionAttempts
        Get
            Return m_maximumConnectionAttempts
        End Get
        Set(ByVal value As Integer)
            If value = -1 OrElse value > 0 Then
                m_maximumConnectionAttempts = value
            Else
                Throw New ArgumentOutOfRangeException("value")
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets a boolean value indicating whether the data exchanged between the server and clients
    ''' will be encrypted using a private session passphrase.
    ''' </summary>
    ''' <value></value>
    ''' <returns>
    ''' True if the data exchanged between the server and clients will be encrypted using a private session 
    ''' passphrase; otherwise False.
    ''' </returns>
    '''<remarks>Handshake and Encryption must be enabled in order to use SecureSession.</remarks>
    <Description("Indicates whether the data exchanged between the server and clients will be encrypted using a private session passphrase."), Category("Security"), DefaultValue(GetType(Boolean), "False")> _
    Public Property SecureSession() As Boolean Implements ICommunicationClient.SecureSession
        Get
            Return m_secureSession
        End Get
        Set(ByVal value As Boolean)
            If (Not value) OrElse _
                    (value AndAlso m_handshake AndAlso m_encryption <> Security.Cryptography.EncryptLevel.None) Then
                m_secureSession = value
            Else
                Throw New InvalidOperationException("Handshake and Encryption must be enabled in order to use SecureSession.")
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets a boolean value indicating whether the server will do a handshake with the client after 
    ''' accepting its connection.
    ''' </summary>
    ''' <value></value>
    ''' <returns>True is the server will do a handshake with the client; otherwise False.</returns>
    ''' <remarks>SecureSession must be disabled before disabling Handshake.</remarks>
    <Description("Indicates whether the server will do a handshake with the client after accepting its connection."), Category("Security"), DefaultValue(GetType(Boolean), "True")> _
    Public Property Handshake() As Boolean Implements ICommunicationClient.Handshake
        Get
            Return m_handshake
        End Get
        Set(ByVal value As Boolean)
            If (value) OrElse (Not value AndAlso Not m_secureSession) Then
                m_handshake = value
                If Not m_handshake Then
                    ' Handshake passphrase will have no effect if handshaking is disabled.
                    m_handshakePassphrase = ""
                End If
            Else
                Throw New ArgumentException("SecureSession must be disabled before disabling Handshake.")
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the passpharse that will be provided to the server for authentication during the handshake 
    ''' process.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The passpharse that will provided to the server for authentication during the handshake process.</returns>
    <Description("The passpharse that will provided to the server for authentication during the handshake process."), Category("Security"), DefaultValue(GetType(String), "")> _
    Public Property HandshakePassphrase() As String Implements ICommunicationClient.HandshakePassphrase
        Get
            Return m_handshakePassphrase
        End Get
        Set(ByVal value As String)
            m_handshakePassphrase = value
            If Not String.IsNullOrEmpty(m_handshakePassphrase) Then
                ' Handshake password has no effect until handshaking is enabled.
                m_handshake = True
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the maximum number of bytes that can be received at a time by the client from the server.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The maximum number of bytes that can be received at a time by the client from the server.</returns>
    <Description("The maximum number of bytes that can be received at a time by the client from the server."), Category("Data"), DefaultValue(GetType(Integer), "8192")> _
    Public Property ReceiveBufferSize() As Integer Implements ICommunicationClient.ReceiveBufferSize
        Get
            Return m_receiveBufferSize
        End Get
        Set(ByVal value As Integer)
            If value > 0 Then
                m_receiveBufferSize = value
            Else
                Throw New ArgumentOutOfRangeException("value")
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the time to wait in seconds for data to be received from the server before timing out.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The time to wait in seconds for data to be received from the server before timing out.</returns>
    ''' <remarks>Set ReceiveTimeout = -1 to disable timeout for receiving data.</remarks>
    <Description("The time to wait in seconds for data to be received from the server before timing out. Set ReceiveTimeout = -1 to disable timeout for receiving data."), Category("Data"), DefaultValue(GetType(Integer), "-1")> _
    Public Property ReceiveTimeout() As Integer Implements ICommunicationClient.ReceiveTimeout
        Get
            Return m_receiveTimeout
        End Get
        Set(ByVal value As Integer)
            If value = -1 OrElse value > 0 Then
                m_receiveTimeout = value
            Else
                Throw New ArgumentOutOfRangeException("value")
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the encryption level to be used for encrypting the data exchanged between the client and 
    ''' server.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The encryption level to be used for encrypting the data exchanged between the client and server.</returns>
    ''' <remarks>
    ''' <para>Set Encryption = None to disable encryption.</para>
    ''' <para>
    ''' The key used for performing cryptography will be different in different senarios.
    ''' 1) HandshakePassphrase will be used as the key when HandshakePassphrase is set and SecureSession is
    '''    disabled.
    ''' 2) DefaultCryptoKey will be used as the key when HandshakePassphrase is not set and SecureSession is
    '''    disabled.
    ''' 3) A private session key will be used as the key when SecureSession is enabled.
    ''' </para>
    ''' </remarks>
    <Description("The encryption level to be used for encrypting the data exchanged between the client and server."), Category("Data"), DefaultValue(GetType(Tva.Security.Cryptography.EncryptLevel), "None")> _
    Public Property Encryption() As Tva.Security.Cryptography.EncryptLevel Implements ICommunicationClient.Encryption
        Get
            Return m_encryption
        End Get
        Set(ByVal value As Tva.Security.Cryptography.EncryptLevel)
            If (Not m_secureSession) OrElse _
                    (m_secureSession AndAlso value <> Security.Cryptography.EncryptLevel.None) Then
                m_encryption = value
            Else
                Throw New ArgumentException("SecureSession session must be disabled before disabling Encryption.")
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the compression level to be used for compressing the data exchanged between the client and 
    ''' server.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The compression level to be used for compressing the data exchanged between the client and server.</returns>
    ''' <remarks>Set Compression = NoCompression to disable compression.</remarks>
    <Description("The compression level to be used for compressing the data exchanged between the client and server."), Category("Data"), DefaultValue(GetType(Tva.IO.Compression.CompressLevel), "NoCompression")> _
    Public Property Compression() As Tva.IO.Compression.CompressLevel Implements ICommunicationClient.Compression
        Get
            Return m_compression
        End Get
        Set(ByVal value As Tva.IO.Compression.CompressLevel)
            m_compression = value
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets a boolean value indicating whether the client is enabled.
    ''' </summary>
    ''' <value></value>
    ''' <returns>True if the client is enabled; otherwise False.</returns>
    <Description("Indicates whether the client is enabled."), Category("Behavior"), DefaultValue(GetType(Boolean), "True")> _
    Public Property Enabled() As Boolean Implements ICommunicationClient.Enabled
        Get
            Return m_enabled
        End Get
        Set(ByVal value As Boolean)
            m_enabled = value
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the encoding to be used for the text sent to the server.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The encoding to be used for the text sent to the server.</returns>
    <Browsable(False)> _
    Public Property TextEncoding() As Encoding Implements ICommunicationClient.TextEncoding
        Get
            Return m_textEncoding
        End Get
        Set(ByVal value As Encoding)
            m_textEncoding = value
        End Set
    End Property

    ''' <summary>
    ''' Gets the protocol used by the client for transferring data to and from the server.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The protocol used by the client for transferring data to and from the server.</returns>
    <Browsable(False)> _
    Public Property Protocol() As TransportProtocol Implements ICommunicationClient.Protocol
        Get
            Return m_protocol
        End Get
        Protected Set(ByVal value As TransportProtocol)
            m_protocol = value
        End Set
    End Property

    ''' <summary>
    ''' Gets the ID of the server to which the client is connected.
    ''' </summary>
    ''' <value></value>
    ''' <returns>ID of the server to which the client is connected.</returns>
    <Browsable(False)> _
    Public Property ServerID() As Guid Implements ICommunicationClient.ServerID
        Get
            Return m_serverID
        End Get
        Protected Set(ByVal value As Guid)
            m_serverID = value
        End Set
    End Property

    ''' <summary>
    ''' Gets the ID of the client.
    ''' </summary>
    ''' <value></value>
    ''' <returns>ID of the client.</returns>
    <Browsable(False)> _
    Public Property ClientID() As Guid Implements ICommunicationClient.ClientID
        Get
            Return m_clientID
        End Get
        Protected Set(ByVal value As Guid)
            m_clientID = value
        End Set
    End Property

    ''' <summary>
    ''' Gets a boolean value indicating whether the client is currently connected to the server.
    ''' </summary>
    ''' <value></value>
    ''' <returns>True if the client is connected; otherwise False.</returns>
    <Browsable(False)> _
    Public ReadOnly Property IsConnected() As Boolean Implements ICommunicationClient.IsConnected
        Get
            Return m_isConnected
        End Get
    End Property

    ''' <summary>
    ''' Gets the time in seconds for which the client has been connected to the server.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The time in seconds for which the client has been connected to the server.</returns>
    <Browsable(False)> _
    Public ReadOnly Property ConnectionTime() As Double Implements ICommunicationClient.ConnectionTime
        Get
            Dim clientConnectionTime As Double = 0
            If m_connectTime > 0 Then
                If m_isConnected Then   ' Client is connected to the server.
                    clientConnectionTime = (System.DateTime.Now.Ticks() - m_connectTime) / 10000000L
                Else    ' Client is not connected to the server.
                    clientConnectionTime = (m_disconnectTime - m_connectTime) / 10000000L
                End If
            End If
            Return clientConnectionTime
        End Get
    End Property

    ''' <summary>
    ''' Gets the total number of bytes sent by the client to the server since the connection is established.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The total number of bytes sent by the client to the server since the connection is established.</returns>
    <Browsable(False)> _
    Public ReadOnly Property TotalBytesSent() As Integer Implements ICommunicationClient.TotalBytesSent
        Get
            Return m_totalBytesSent
        End Get
    End Property

    ''' <summary>
    ''' Gets the total number of bytes received by the client from the server since the connection is established.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The total number of bytes received by the client from the server since the connection is established.</returns>
    <Browsable(False)> _
    Public ReadOnly Property TotalBytesReceived() As Integer Implements ICommunicationClient.TotalBytesReceived
        Get
            Return m_totalBytesReceived
        End Get
    End Property

    ''' <summary>
    ''' Gets the current status of the client.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The current status of the client.</returns>
    <Browsable(False)> _
    Public ReadOnly Property Status() As String Implements ICommunicationClient.Status
        Get
            With New StringBuilder()
                .Append("             Server ID: " & ServerID.ToString())
                .Append(Environment.NewLine())
                .Append("             Client ID: " & ClientID().ToString())
                .Append(Environment.NewLine())
                .Append("          Client state: " & IIf(IsConnected(), "Connected", "Not Connected"))
                .Append(Environment.NewLine())
                .Append("       Connection time: " & SecondsToText(ConnectionTime()))
                .Append(Environment.NewLine())
                .Append("        Receive buffer: " & ReceiveBufferSize.ToString())
                .Append(Environment.NewLine())
                .Append("    Transport protocol: " & Protocol.ToString())
                .Append(Environment.NewLine())
                .Append("    Text encoding used: " & TextEncoding.EncodingName())
                .Append(Environment.NewLine())
                .Append("      Total bytes sent: " & TotalBytesSent())
                .Append(Environment.NewLine())
                .Append("  Total bytes received: " & TotalBytesReceived())
                .Append(Environment.NewLine())
                Return .ToString()
            End With
        End Get
    End Property

    ''' <summary>
    ''' Connects the client to the server.
    ''' </summary>
    Public MustOverride Sub Connect() Implements ICommunicationClient.Connect

    ''' <summary>
    ''' Cancels connecting to the server.
    ''' </summary>
    Public MustOverride Sub CancelConnect() Implements ICommunicationClient.CancelConnect

    ''' <summary>
    ''' Disconnects the client from the server it is connected to.
    ''' </summary>
    Public MustOverride Sub Disconnect() Implements ICommunicationClient.Disconnect

    ''' <summary>
    ''' Sends data to the server.
    ''' </summary>
    ''' <param name="data">The plain-text data that is to be sent to the server.</param>
    Public Overridable Sub Send(ByVal data As String) Implements ICommunicationClient.Send

        Send(m_textEncoding.GetBytes(data))

    End Sub

    ''' <summary>
    ''' Sends data to the server.
    ''' </summary>
    ''' <param name="serializableObject">The serializable object that is to be sent to the server.</param>
    Public Overridable Sub Send(ByVal serializableObject As Object) Implements ICommunicationClient.Send

        Send(GetBytes(serializableObject))

    End Sub

    ''' <summary>
    ''' Sends data to the server.
    ''' </summary>
    ''' <param name="data">The binary data that is to be sent to the server.</param>
    Public Overridable Sub Send(ByVal data As Byte()) Implements ICommunicationClient.Send

        If m_enabled AndAlso m_isConnected Then
            If data IsNot Nothing AndAlso data.Length() > 0 Then
                data = GetPreparedData(data)
                If data.Length() <= MaximumDataSize Then
                    'SendPreparedData(data)
                    ' Begin sending data on a seperate thread.
                    Tva.Threading.RunThread.ExecuteNonPublicMethod(Me, "SendPreparedData", data)
                Else
                    ' Prepared data is too large to be sent.
                    Throw New ArgumentException("Size of the data to be sent exceeds the maximum data size of " & MaximumDataSize & " bytes.")
                End If
            Else
                Throw New ArgumentNullException("data")
            End If
        End If

    End Sub

    ''' <summary>
    ''' The key used for encryption and decryption when Encryption is enabled but HandshakePassphrase is not set.
    ''' </summary>
    Protected Const DefaultCryptoKey As String = "6572a33d-826f-4d96-8c28-8be66bbc700e"

    ''' <summary>
    ''' Raises the Tva.Communication.ClientBase.Connecting event.
    ''' </summary>
    ''' <param name="e">An System.EventArgs that contains the event data.</param>
    ''' <remarks>This method is to be called when the client is attempting connection to the server.</remarks>
    Protected Sub OnConnecting(ByVal e As EventArgs)

        RaiseEvent Connecting(Me, e)

    End Sub

    ''' <summary>
    ''' Raises the Tva.Communication.ClientBase.ConnectingCancelled event.
    ''' </summary>
    ''' <param name="e">An System.EventArgs that contains the event data.</param>
    ''' <remarks>
    ''' This method is to be called when attempts for connecting the client to the server are stopped on user's
    ''' request (i.e. When CancelConnect() is called before client is connected to the server).
    ''' </remarks>
    Protected Sub OnConnectingCancelled(ByVal e As EventArgs)

        RaiseEvent ConnectingCancelled(Me, e)

    End Sub

    ''' <summary>
    ''' Raises the Tva.Communication.ClientBase.ConnectingException event.
    ''' </summary>
    ''' <param name="ex">The exception that was encountered while connecting to the server.</param>
    ''' <remarks>
    ''' This method is to be called when all attempts for connecting to the server have been made but failed 
    ''' due to exceptions.
    ''' </remarks>
    Protected Sub OnConnectingException(ByVal ex As Exception)

        RaiseEvent ConnectingException(ex)

    End Sub

    ''' <summary>
    ''' Raises the Tva.Communication.ClientBase.Connected event.
    ''' </summary>
    ''' <param name="e">An System.EventArgs that contains the event data.</param>
    ''' <remarks>This method is to be called when the client has successfully connected to the server.</remarks>
    Protected Sub OnConnected(ByVal e As EventArgs)

        m_isConnected = True
        m_connectTime = Date.Now.Ticks  ' Save the time when the client connected to the server.
        m_disconnectTime = 0
        m_totalBytesSent = 0    ' Reset the number of bytes sent and received between the client and server.
        m_totalBytesReceived = 0
        RaiseEvent Connected(Me, e)

    End Sub

    ''' <summary>
    ''' Raises the Tva.Communication.ClientBase.Disconnected event.
    ''' </summary>
    ''' <param name="e">An System.EventArgs that contains the event data.</param>
    ''' <remarks>This method is to be called when the client has disconnected from the server.</remarks>
    Protected Sub OnDisconnected(ByVal e As EventArgs)

        m_serverID = Guid.Empty
        m_isConnected = False
        m_disconnectTime = Date.Now.Ticks() ' Save the time when client was disconnected from the server.
        RaiseEvent Disconnected(Me, e)

    End Sub

    ''' <summary>
    ''' Raises the Tva.Communication.ClientBase.SendBegin event.
    ''' </summary>
    ''' <param name="data">The data being sent to the server.</param>
    ''' <remarks>This method is to be called when the client begins sending data to the server.</remarks>
    Protected Sub OnSendDataBegin(ByVal data As Byte())

        RaiseEvent SendDataBegin(data)

    End Sub

    ''' <summary>
    ''' Raises the Tva.Communication.ClientBase.SendComplete event.
    ''' </summary>
    ''' <param name="data">The data sent to the server.</param>
    ''' <remarks>This method is to be called when the client has finished sending data to the server.</remarks>
    Protected Sub OnSendDataComplete(ByVal data As Byte())

        m_totalBytesSent += data.Length()
        RaiseEvent SendDataComplete(data)

    End Sub

    ''' <summary>
    ''' Raises the Tva.Communication.ClientBase.ReceivedData event.
    ''' </summary>
    ''' <param name="data">The data that was received from the server.</param>
    ''' <remarks>This method is to be called when the client receives data from the server.</remarks>
    Protected Sub OnReceivedData(ByVal data As Byte())

        m_totalBytesReceived += data.Length()

        Try
            data = GetActualData(data)
        Catch ex As Exception
            ' We'll just pass on the data that we received.
        End Try
        RaiseEvent ReceivedData(data)

    End Sub

    ''' <summary>
    ''' Raises the Tva.Communication.ClientBase.ReceiveTimedOut event.
    ''' </summary>
    ''' <param name="e">An System.EventArgs that contains the event data.</param>
    ''' <remarks>
    ''' This method is to be called when no data is received from the server after waiting for the specified time.
    ''' </remarks>
    Protected Sub OnReceiveTimedOut(ByVal e As EventArgs)

        RaiseEvent ReceiveTimedOut(Me, e)

    End Sub

    ''' <summary>
    ''' Performs the necessary compression and encryption on the specified data and returns it.
    ''' </summary>
    ''' <param name="data">The data on which compression and encryption is to be performed.</param>
    ''' <returns>Compressed and encrypted data.</returns>
    ''' <remarks>No encryption is performed if SecureSession is enabled, even if Encryption is enabled.</remarks>
    Protected Function GetPreparedData(ByVal data As Byte()) As Byte()

        data = CompressData(data, m_compression)
        If Not m_secureSession Then
            Dim key As String = m_handshakePassphrase
            If String.IsNullOrEmpty(key) Then
                key = DefaultCryptoKey
            End If
            data = EncryptData(data, key, m_encryption)
        End If
        Return data

    End Function

    ''' <summary>
    ''' Performs the necessary uncompression and decryption on the specified data and returns it.
    ''' </summary>
    ''' <param name="data">The data on which uncompression and decryption is to be performed.</param>
    ''' <returns>Uncompressed and decrypted data.</returns>
    ''' <remarks>No decryption is performed if SecureSession is enabled, even if Encryption is enabled.</remarks>
    Protected Function GetActualData(ByVal data As Byte()) As Byte()

        If Not m_secureSession Then
            Dim key As String = m_handshakePassphrase
            If String.IsNullOrEmpty(key) Then
                key = DefaultCryptoKey
            End If
            data = DecryptData(data, key, m_encryption)
        End If
        data = UncompressData(data, m_compression)
        Return data

    End Function

    ''' <summary>
    ''' Sends prepared data to the server.
    ''' </summary>
    ''' <param name="data">The prepared data that is to be sent to the server.</param>
    Protected MustOverride Sub SendPreparedData(ByVal data As Byte())

    ''' <summary>
    ''' Determines whether specified connection string required for the client to connect to the server is valid.
    ''' </summary>
    ''' <param name="connectionString">The connection string to be validated.</param>
    ''' <returns>True is the connection string is valid; otherwise False.</returns>
    Protected MustOverride Function ValidConnectionString(ByVal connectionString As String) As Boolean

End Class