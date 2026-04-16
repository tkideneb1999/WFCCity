using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CityBuilder
{
    public class LandBuilder : MonoBehaviour
    {
        
        private bool _enabled = true;
        public bool Enabled => _enabled;

        [SerializeField]
        private string _landPartName;
        ushort _landPartId;

        [SerializeField]
        private Grid grid;

        [SerializeField]
        private Camera gameCamera;

        [SerializeField]
        private InputActionAsset InputActions;

        private InputAction _screenPosAction;
        private InputAction _leftClickAction;
        private InputAction _rightClickAction;

        [SerializeField]
        private GameObject _previewPrefab;
        [SerializeField]
        private Vector3 _centerOffset = new Vector3(2, 0, 2);
        private GameObject _previewObject;

        // TODO: Remove
        [SerializeField]
        private GameObject _landTestPrefab;

        private Plane _worldPlane;

        private bool _selectedValidCell;
        private Vector2Int _selectedCell;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // TODO: Move this
            InputActions.FindActionMap("Default").Enable();

            _screenPosAction = InputActions.FindAction("ScreenPos");

            _leftClickAction = InputActions.FindAction("LeftClick");
            _leftClickAction.performed += AddLand;

            _rightClickAction = InputActions.FindAction("RightClick");
            _rightClickAction.performed += RemoveLand;

            _worldPlane = new Plane(Vector3.up, 0);

            _previewObject = GameObject.Instantiate(_previewPrefab);
            _previewObject.SetActive(false);

            _landPartId = PartRegistry.Instance.GetPartId(_landPartName);

        }

        private void AddLand(InputAction.CallbackContext obj)
        {
            if (!_selectedValidCell)
                return;

            grid.SetEditingData(_selectedCell.x, 0, _selectedCell.y, _landPartId);
        }

        private void RemoveLand(InputAction.CallbackContext obj)
        {
            if(!_selectedValidCell)
                return;


        }

        // Update is called once per frame
        void Update()
        {
            if(!_enabled)
            {
                return;
            }

            Vector3 screenPos = _screenPosAction.ReadValue<Vector2>();
            Ray ray = gameCamera.ScreenPointToRay(screenPos);

            if(!_worldPlane.Raycast(ray, out float distance))
            {
                _selectedValidCell = false;
                _previewObject.SetActive(false);
                return;
            }

            Vector3 intersectPoint = (gameCamera.transform.position + ray.direction * distance);
            Vector3 negOffset = new Vector3(
                intersectPoint.x < 0f ? grid.CellSize : 0,
                0,
                intersectPoint.z < 0f ? grid.CellSize : 0
                );
            intersectPoint -= negOffset;

            intersectPoint /= grid.CellSize;

            _selectedCell = new Vector2Int((int)intersectPoint.x, (int)intersectPoint.z);

            intersectPoint.x = _selectedCell.x;
            intersectPoint.y = (int)intersectPoint.y;
            intersectPoint.z = _selectedCell.y;

            _selectedValidCell = true;
            
            intersectPoint *= grid.CellSize;
            intersectPoint += _centerOffset;

            _previewObject.SetActive(true);
            _previewObject.transform.position = intersectPoint;
        }
    }
}
