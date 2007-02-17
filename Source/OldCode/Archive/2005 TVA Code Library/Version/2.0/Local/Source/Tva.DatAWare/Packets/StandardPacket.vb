Imports System.Text

Namespace Packets

    Public Class StandardPacket
        Inherits PacketBase

        ' *******************************************************
        ' *                 Packet structure                    *
        ' *******************************************************
        ' * # Of Bytes  Byte Index  Data Type   Description     *
        ' * ----------  ----------  ----------  -----------     *
        ' * 2           0-1         Int16       Packet ID       *
        ' * 4           2-5         Int32       Database index  *
        ' * 8           6-13        Double      Time-tag        *
        ' * 4           14-17       Int32       Quality         *
        ' * 4           18-21       Single      Value           *
        ' *******************************************************

#Region " Member Declaration "

        Private m_index As Integer
        Private m_timeTag As Double
        Private m_quality As Integer
        Private m_value As Single

#End Region

        Public Shadows Const TypeID As Short = 1
        Public Shadows Const BinaryLength As Integer = 22

        Public Sub New()

            MyBase.New()
            MyBase.ActionType = PacketActionType.SaveAndReply
            MyBase.SaveLocation = PacketSaveLocation.ArchiveFile

        End Sub

        Public Sub New(ByVal binaryImage As Byte())

            MyClass.New(binaryImage, 0)

        End Sub

        Public Sub New(ByVal binaryImage As Byte(), ByVal startIndex As Integer)

            MyClass.New()

            If (binaryImage.Length - startIndex) >= BinaryLength Then
                ' We have a binary image of valid size.
                Dim packetTypeID As Short = BitConverter.ToInt16(binaryImage, startIndex)
                If packetTypeID = TypeID Then
                    ' We have a binary image with the correct type ID.
                    m_index = BitConverter.ToInt32(binaryImage, startIndex + 2)
                    m_timeTag = BitConverter.ToDouble(binaryImage, startIndex + 6)
                    m_quality = BitConverter.ToInt32(binaryImage, startIndex + 14)
                    m_value = BitConverter.ToSingle(binaryImage, startIndex + 18)
                Else
                    Throw New ArgumentException(String.Format("Unexpected packet type ID {0}. Expected packet type ID {1}.", packetTypeID, TypeID))
                End If
            Else
                Throw New ArgumentException(String.Format("Binary image smaller than expected. Expected binary image size {0}.", BinaryLength))
            End If

        End Sub

        Public Sub New(ByVal index As Integer, ByVal timeTag As Double, ByVal quality As Integer, _
                ByVal value As Single)

            MyClass.New()
            
            m_index = index
            m_timeTag = timeTag
            m_quality = quality
            m_value = value

        End Sub

        Public Property Index() As Integer
            Get
                Return m_index
            End Get
            Set(ByVal value As Integer)
                m_index = value
            End Set
        End Property

        Public Property TimeTag() As Double
            Get
                Return m_timeTag
            End Get
            Set(ByVal value As Double)
                m_timeTag = value
            End Set
        End Property

        Public Property Quality() As Integer
            Get
                Return m_quality
            End Get
            Set(ByVal value As Integer)
                m_quality = value
            End Set
        End Property

        Public Property Value() As Single
            Get
                Return m_value
            End Get
            Set(ByVal value As Single)
                m_value = value
            End Set
        End Property

        Public Overrides Function GetReplyData() As Byte()

            Return Encoding.ASCII.GetBytes("ACK")

        End Function

        Public Overrides Function GetSaveData() As Byte()

            Return New PointData(m_timeTag, m_value, m_quality).BinaryImage

        End Function

        Public Shared Shadows Function TryParse(ByVal binaryImage As Byte(), ByRef packets As List(Of IPacket)) As Boolean

            If binaryImage IsNot Nothing Then
                If binaryImage.Length >= BinaryLength Then
                    packets = New List(Of IPacket)()
                    For i As Integer = 0 To binaryImage.Length - 1 Step BinaryLength
                        Try
                            packets.Add(New StandardPacket(binaryImage, i))
                        Catch ex As Exception
                            ' Its likely that we encounter an exception here if the entire buffer is not well formed.
                        End Try
                    Next
                    Return True
                End If
            End If

            Return False

        End Function

    End Class

End Namespace