namespace ImprovedPublicTransport.Integration.TrainDisplayUpdated
{
    using System;
    using System.Reflection;

    internal static class FpsCameraIntegration
    {
        private const string ControllerTypeName = "FPSCamera.Cam.Controller.FPSCamController";
        private const string VehicleCamTypeName = "FPSCamera.Cam.VehicleCam";
        private static Type _vehicleCamType;
        private static PropertyInfo _instanceProperty;
        private static PropertyInfo _fpsCamProperty;
        private static PropertyInfo _followIdProperty;
        private static bool _resolved;

        internal static bool TryGetVehicle(out ushort vehicleId)
        {
            vehicleId = 0;
            if (!_resolved && !TryResolve())
                return false;

            try
            {
                var controller = _instanceProperty.GetValue(null, null);
                var camera = controller == null ? null : _fpsCamProperty.GetValue(controller, null);
                if (camera == null || !_vehicleCamType.IsInstanceOfType(camera))
                    return false;

                var followId = (uint)_followIdProperty.GetValue(camera, null);
                if (followId == 0 || followId > ushort.MaxValue)
                    return false;

                vehicleId = (ushort)followId;
                return TrainDisplayIntegration.IsSupportedVehicle(vehicleId);
            }
            catch
            {
                Clear();
                return false;
            }
        }

        internal static void Clear()
        {
            _vehicleCamType = null;
            _instanceProperty = null;
            _fpsCamProperty = null;
            _followIdProperty = null;
            _resolved = false;
        }

        private static bool TryResolve()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Assembly assembly = null;
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (assemblies[i].GetName().Name == "FPSCamera")
                {
                    assembly = assemblies[i];
                    break;
                }
            }

            if (assembly == null)
                return false;

            var controllerType = assembly.GetType(ControllerTypeName, false);
            _vehicleCamType = assembly.GetType(VehicleCamTypeName, false);
            if (controllerType == null || _vehicleCamType == null)
            {
                Clear();
                return false;
            }

            _instanceProperty = controllerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            _fpsCamProperty = controllerType.GetProperty("FPSCam", BindingFlags.Public | BindingFlags.Instance);
            _followIdProperty = _vehicleCamType.GetProperty("FollowID", BindingFlags.Public | BindingFlags.Instance);
            _resolved = _instanceProperty != null && _fpsCamProperty != null && _followIdProperty != null
                && _followIdProperty.PropertyType == typeof(uint);
            if (!_resolved)
                Clear();
            return _resolved;
        }
    }
}
