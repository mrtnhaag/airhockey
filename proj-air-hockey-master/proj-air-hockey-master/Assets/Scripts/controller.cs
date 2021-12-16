// GENERATED AUTOMATICALLY FROM 'Assets/Scripts/controller.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @Controller : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @Controller()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""controller"",
    ""maps"": [
        {
            ""name"": ""Controlleractions"",
            ""id"": ""ad53f11c-d016-4817-bd15-f9252b0acb23"",
            ""actions"": [
                {
                    ""name"": ""move"",
                    ""type"": ""Value"",
                    ""id"": ""2793595e-1ce4-4f08-a729-175fd068d056"",
                    ""expectedControlType"": ""Stick"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""03d213b8-d07a-42d1-bff8-2a2d0989033b"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Controlleractions
        m_Controlleractions = asset.FindActionMap("Controlleractions", throwIfNotFound: true);
        m_Controlleractions_move = m_Controlleractions.FindAction("move", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // Controlleractions
    private readonly InputActionMap m_Controlleractions;
    private IControlleractionsActions m_ControlleractionsActionsCallbackInterface;
    private readonly InputAction m_Controlleractions_move;
    public struct ControlleractionsActions
    {
        private @Controller m_Wrapper;
        public ControlleractionsActions(@Controller wrapper) { m_Wrapper = wrapper; }
        public InputAction @move => m_Wrapper.m_Controlleractions_move;
        public InputActionMap Get() { return m_Wrapper.m_Controlleractions; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(ControlleractionsActions set) { return set.Get(); }
        public void SetCallbacks(IControlleractionsActions instance)
        {
            if (m_Wrapper.m_ControlleractionsActionsCallbackInterface != null)
            {
                @move.started -= m_Wrapper.m_ControlleractionsActionsCallbackInterface.OnMove;
                @move.performed -= m_Wrapper.m_ControlleractionsActionsCallbackInterface.OnMove;
                @move.canceled -= m_Wrapper.m_ControlleractionsActionsCallbackInterface.OnMove;
            }
            m_Wrapper.m_ControlleractionsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @move.started += instance.OnMove;
                @move.performed += instance.OnMove;
                @move.canceled += instance.OnMove;
            }
        }
    }
    public ControlleractionsActions @Controlleractions => new ControlleractionsActions(this);
    public interface IControlleractionsActions
    {
        void OnMove(InputAction.CallbackContext context);
    }
}
