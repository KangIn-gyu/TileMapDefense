using UnityEngine;

public interface IInputReceiver
{
    public void RegisterToInputHandler(InputHandlerManager _inputHandler);
    public void UnregisterFromInputHandler(InputHandlerManager _inputHandler);
}