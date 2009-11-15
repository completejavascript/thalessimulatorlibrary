﻿''
'' This program is free software; you can redistribute it and/or modify
'' it under the terms of the GNU General Public License as published by
'' the Free Software Foundation; either version 2 of the License, or
'' (at your option) any later version.
''
'' This program is distributed in the hope that it will be useful,
'' but WITHOUT ANY WARRANTY; without even the implied warranty of
'' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'' GNU General Public License for more details.
''
'' You should have received a copy of the GNU General Public License
'' along with this program; if not, write to the Free Software
'' Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
'' 

Imports ThalesSim.Core.Cryptography

Namespace ConsoleCommands

    ''' <summary>
    ''' This is the base class from which all console commands are derived.
    ''' </summary>
    ''' <remarks>Console commands are activated by a specific code from the
    ''' console. After requesting a series of information all commands produce
    ''' a result and terminate.
    ''' </remarks>
    Public MustInherit Class AConsoleCommand

        ''' <summary>
        ''' 16 zeroes.
        ''' </summary>
        ''' <remarks>
        ''' 16 zeroes.
        ''' </remarks>
        Protected Const ZEROES As String = "0000000000000000"

        ''' <summary>
        ''' Indicates whether a command has finished processing.
        ''' </summary>
        ''' <remarks></remarks>
        Protected _commandFinished As Boolean = False

        ''' <summary>
        ''' Stack of messages to send to the console.
        ''' </summary>
        ''' <remarks></remarks>
        Protected _stack As ConsoleMessageStack = New ConsoleMessageStack

        ''' <summary>
        ''' Stack of messages already send to the console and responded to by the user.
        ''' </summary>
        ''' <remarks></remarks>
        Protected _inStack As ConsoleMessageStack = New ConsoleMessageStack

        ''' <summary>
        ''' Current message being processed.
        ''' </summary>
        ''' <remarks></remarks>
        Protected _curMessage As ConsoleMessage = Nothing

        ''' <summary>
        ''' True if the implementor needs a variable number of key components.
        ''' </summary>
        ''' <remarks></remarks>
        Protected hasComponents As Boolean = False

        ''' <summary>
        ''' Number of key components needed by the implementor.
        ''' </summary>
        ''' <remarks></remarks>
        Protected numComponents As Integer

        ''' <summary>
        ''' Determines whether processing of the command has finished.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property CommandFinished() As Boolean
            Get
                Return (_stack.MessagesOnStack = 0)
            End Get
        End Property

        ''' <summary>
        ''' Returns True if the implemented command requires no input.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function IsNoinputCommand() As Boolean
            Return (_stack.MessagesOnStack = 0)
        End Function

        ''' <summary>
        ''' Called to acquire the next message that will be send to the console.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetClientMessage() As String
            'Get the next message from the stack.
            _curMessage = _stack.PopFromStack

            'Do we have multiple components?
            If hasComponents Then
                'Yes, is this message one that needs multiple components?
                If _curMessage.IsComponent Then
                    'Yes. We'll push to the stack more messages to make up the number of required components.

                    'Off-by-one because we're pushing first component to client already
                    For i As Integer = 0 To numComponents - 2
                        Dim newMsg As New ConsoleMessage(_curMessage.ClientMessage + " (component #" + (i + 2).ToString + ")", _
                                                         _curMessage.ConsoleMessage, False, False, _curMessage.ConsoleMessageValidator)
                    Next
                    Return _curMessage.ClientMessage + " (component #1)"
                End If
            End If
            Return _curMessage.ClientMessage
        End Function

        ''' <summary>
        ''' Called when a message from the console arrives.
        ''' </summary>
        ''' <param name="consoleMsg">String message from console.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function AcceptMessage(ByVal consoleMsg As String) As String
            _curMessage.ConsoleMessage = consoleMsg

            'Do we validate it?
            If _curMessage.ConsoleMessageValidator IsNot Nothing Then
                Try
                    _curMessage.ConsoleMessageValidator.ValidateConsoleMessage(_curMessage.ConsoleMessage)
                Catch ex As Exception
                    'Validation error, pop everything and return error message.
                    While _stack.MessagesOnStack <> 0
                        _stack.PopFromStack()
                    End While
                    Return ex.Message
                End Try
            End If

            'Find out the number of components.
            If _curMessage.IsNumberOfComponents Then
                hasComponents = True
                numComponents = Convert.ToInt32(_curMessage.ConsoleMessage)
            End If

            'Push to the incoming stack so the implementor can find it.
            _inStack.PushToStack(_curMessage)

            'Done with messages?
            If _stack.MessagesOnStack = 0 Then
                'Yes, perform the actual processing.
                Return ProcessMessage()
            Else
                Return Nothing
            End If

        End Function

        ''' <summary>
        ''' Called during initialization to setup the message stack.
        ''' </summary>
        ''' <remarks></remarks>
        Public MustOverride Sub InitializeStack()

        ''' <summary>
        ''' Called when all messages have been exchanged. Implementations should place the command processing here.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride Function ProcessMessage() As String

        ''' <summary>
        ''' Parses and validates a key type code.
        ''' </summary>
        ''' <remarks>
        ''' This method parses a key type code. If all is well, the <b>Pair</b> and <b>Var</b> 
        ''' output variables. Otherwise, the method throws an exception.
        ''' </remarks>
        Protected Sub ValidateKeyTypeCode(ByVal ktc As String, _
                                          ByRef Pair As LMKPairs.LMKPair, _
                                          ByRef Var As String)

            KeyTypeTable.ParseKeyTypeCode(ktc, Pair, Var)

        End Sub

        ''' <summary>
        ''' Parses and validates a key scheme code.
        ''' </summary>
        ''' <remarks>
        ''' This method parses a key scheme code. If all is well, the <b>KS</b> output variable
        ''' is set. Otherwise, an the method throws an exception.
        ''' </remarks>
        Protected Sub ValidateKeySchemeCode(ByVal ksc As String, _
                                            ByRef KS As KeySchemeTable.KeyScheme)

            KS = KeySchemeTable.GetKeySchemeFromValue(ksc)

        End Sub

        ''' <summary>
        ''' Validates a key scheme combined with a key length.
        ''' </summary>
        ''' <param name="keyLen">Key length (1,2,3)</param>
        ''' <param name="keyScheme">Key scheme (0,X,T,Y,U)</param>
        ''' <param name="ks">KeyScheme variable set according to previous parameters.</param>
        ''' <remarks>This method sets the value of the ks parameter according to the values of keyLen and keyScheme.
        ''' If keyLen and keyScheme don't match (for example, keyLen=1 and keyScheme=X), an exception is thrown.</remarks>
        Protected Sub ValidateKeySchemeAndLength(ByVal keyLen As String, ByVal keyScheme As String, ByRef ks As KeySchemeTable.KeyScheme)

            Select Case keyLen
                Case "1"
                    ks = KeySchemeTable.KeyScheme.SingleDESKey
                    If keyScheme <> "0" Then
                        Throw New Exceptions.XInvalidKeyScheme("INVALID KEY SCHEME FOR KEY LENGTH")
                    End If
                Case "2"
                    Select Case keyScheme
                        Case "U"
                            ks = KeySchemeTable.KeyScheme.DoubleLengthKeyVariant
                        Case "X"
                            ks = KeySchemeTable.KeyScheme.DoubleLengthKeyVariant
                        Case Else
                            Throw New Exceptions.XInvalidKeyScheme("INVALID KEY SCHEME FOR KEY LENGTH")
                    End Select
                Case "3"
                    Select Case keyScheme
                        Case "X"
                            ks = KeySchemeTable.KeyScheme.TripleLengthKeyAnsi
                        Case "T"
                            ks = KeySchemeTable.KeyScheme.TripleLengthKeyVariant
                        Case Else
                            Throw New Exceptions.XInvalidKeyScheme("INVALID KEY SCHEME FOR KEY LENGTH")
                    End Select
            End Select
        End Sub

        ''' <summary>
        ''' Determines whether function requirements are met.
        ''' </summary>
        ''' <remarks>
        ''' If the function requirements are met for the specified parameters, the method returns.
        ''' Otherwise, an exception is thrown.
        ''' </remarks>
        Protected Sub ValidateFunctionRequirement(ByVal func As KeyTypeTable.KeyFunction, ByVal Pair As LMKPairs.LMKPair, ByVal var As String)

            Dim requirement As KeyTypeTable.AuthorizedStateRequirement = KeyTypeTable.GetAuthorizedStateRequirement(KeyTypeTable.KeyFunction.Generate, Pair, var)
            If requirement = KeyTypeTable.AuthorizedStateRequirement.NotAllowed Then
                Throw New Exceptions.XFunctionNotPermitted("FUNCTION NOT PERMITTED")
            ElseIf requirement = KeyTypeTable.AuthorizedStateRequirement.NeedsAuthorizedState AndAlso CType(Resources.GetResource(Resources.AUTHORIZED_STATE), Boolean) = False Then
                Throw New Exceptions.XNeedsAuthorizedState("NOT AUTHORIZED")
            End If

        End Sub

        ''' <summary>
        ''' Formats a key in a way that is easier to read.
        ''' </summary>
        ''' <param name="key">Original key.</param>
        ''' <returns>Formatted key.</returns>
        ''' <remarks></remarks>
        Protected Function MakeKeyPresentable(ByVal key As String) As String
            Dim out As String = ""
            Dim inIdx As Integer = 0
            If key.Length Mod 16 <> 0 Then
                out = key.Substring(0, 1) + " "
                inIdx = 1
            End If

            While inIdx < key.Length
                out = out + key.Substring(inIdx, 4) + " "
                inIdx += 4
            End While

            Return out
        End Function

        ''' <summary>
        ''' Formats a key check value in a way that is easier to read.
        ''' </summary>
        ''' <param name="cv">Original check value.</param>
        ''' <returns>Formatted check value.</returns>
        ''' <remarks></remarks>
        Protected Function MakeCheckValuePresentable(ByVal cv As String) As String
            Return cv.Substring(0, 4) + " " + cv.Substring(5, 2)
        End Function

    End Class

End Namespace