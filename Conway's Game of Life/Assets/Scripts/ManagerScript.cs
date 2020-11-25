using System.Collections;
using UnityEngine;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation

public class ManagerScript : MonoBehaviour
{
    public bool gridVisibility = true;
    public int gridWidth = 6;
    public int gridHeight = 6;
    public int tickRate = 1;
    public Color gridOutline = Color.black;
    public GameObject lineSet;
    public Material gridMaterial;
    
    public new Camera camera;
    
    private bool _isEnabled;
    private GameObject[,] _cubes;
    private bool[,] _cells; //cell array controls cubes array
    
    bool _alive = true;
    bool dead = false;

    private void Start()
    {
        _cubes = new GameObject[gridWidth, gridHeight]; 
        _cells = new bool[gridWidth, gridHeight];

        ArrangeCamera(); //arrange camera 
        GridFormation();    //create grid
        if (gridVisibility)
        {
            LayoutFormation(); // create layout lines
        }

        StartCoroutine(Ticking()); //tick control
    }

    private void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            ToggleCellState(); // used to change the state of cell
        }

        if (Input.GetButtonDown("Jump"))
        {
            _isEnabled = !_isEnabled; //used to control tick
        }
    }

    /// <summary>
    /// arrange camera according to grid dimension
    /// </summary>
    /// <returns></returns>
    private void ArrangeCamera()
    {
      
        camera.transform.position = new Vector3(gridWidth / 2f, gridHeight / 2f);
        camera.orthographicSize = gridHeight / 2 + 2;
    }
    /// <summary>
    /// form a seed consist of 4 pixels at center
    /// </summary>
    /// <returns></returns>
    private void SeedFormation() 
    {
        _cells[gridWidth / 2, gridHeight / 2] = true;
        _cells[(gridWidth/2) -1 , (gridHeight/2) - 1] = true;
        _cells[(gridWidth/2) - 1, gridHeight/2]= true;
        _cells[gridWidth/2, (gridHeight/2) - 1] = true;
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                _cubes[x, y].GetComponent<MeshRenderer>().enabled = _cells[x, y];
            }
        }
    }
    /// <summary>
    /// form cells as grid
    /// </summary>
    /// <returns></returns>
    private void GridFormation()
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                CreateCube(x, y);
            }
        }

        SeedFormation();
    }
    
    /// <summary>
    /// draw grid according to dimension
    /// </summary>
    /// <returns></returns>
    private void LayoutFormation()
    {
        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 startPos = new Vector3(0, y, 1.5f);
            Vector3 endPos = new Vector3(gridWidth, y, 1.5f);
            DrawLine(startPos, endPos);
        }

        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 startPos = new Vector3(x, 0, 1.5f);
            Vector3 endPos = new Vector3(x, gridHeight, 1.5f);
            DrawLine(startPos, endPos);
        }
    }

    /// <summary>
    /// draw line by line
    /// </summary>
    /// <returns></returns>
    private void DrawLine(Vector3 startPos, Vector3 endPos)
    {
        GameObject myLine = new GameObject();
        myLine.transform.position = startPos;
        myLine.transform.parent = lineSet.transform;
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        lr.material = gridMaterial;
        lr.startColor = gridOutline;
        lr.endColor = gridOutline;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.SetPosition(0, startPos);
        lr.SetPosition(1, endPos);
    }

    /// <summary>
    /// tick control specified by user
    /// </summary>
    /// <returns></returns>
    private IEnumerator Ticking()
    {
        while (true)
        {
            if (_isEnabled)
            {
                UpdateGrid(Generation());
            }
            yield return new WaitForSeconds(tickRate);
        }
        // ReSharper disable once IteratorNeverReturns
    }

    /// <summary>
    /// used to change state of pixel at run time by clicking on it
    /// </summary>
    /// <returns></returns>
    void ToggleCellState()
    {
        // ReSharper disable once Unity.PerformanceCriticalCodeCameraMain
        if (!(Camera.main is null))
        {
            // ReSharper disable once Unity.PerformanceCriticalCodeCameraMain
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit, 100))
            {
                int x = (int)Mathf.Floor(hit.point.x);
                int y = (int)Mathf.Floor(hit.point.y);
                _cells[x, y] = !_cells[x, y];
                _cubes[x, y].GetComponent<MeshRenderer>().enabled = _cells[x, y]; // enable visibility of cell
            }
        }
    }

    /// <summary>
    /// setup single cube at desired location
    /// </summary>
    /// <returns></returns>
    void CreateCube(int x, int y)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = new Vector3(x + 0.5f, y + 0.5f, 1);
        cube.transform.localScale = new Vector3(0.98f,0.98f, 1);
        cube.transform.parent = this.transform;
        cube.GetComponent<MeshRenderer>().enabled = false; //cells are dead at initial stage
        _cubes[x, y] = cube;
    }
    
    /// <summary>
    /// next generation according to rules
    /// </summary>
    /// <returns></returns>
    bool[,] Generation()
    {
        bool[,] nextGenCells = new bool[gridWidth, gridHeight];

        for (int y = 1; y < gridHeight - 1; y++)
        {
            for (int x = 1; x < gridWidth - 1; x++)
            {
                int neighbors = GetNumOfNeighbors(x, y);

                if (IsUnderpopulated(neighbors) || IsOverpopulated(neighbors))
                {
                    nextGenCells[x, y] = dead;
                }
                else
                {
                    if (CanReproduce(neighbors, x, y))
                    {
                        nextGenCells[x, y] = _alive;
                    }
                    else if (!_cells[x, y])
                    {
                        nextGenCells[x, y] = dead;
                    }
                    else
                    {
                        nextGenCells[x, y] = _alive;
                    }
                }
            }
        }

        return nextGenCells;
    }
    
    /// <summary>
    /// update grid keeping rules valid
    /// </summary>
    /// <returns></returns>
    void UpdateGrid(bool[,] nextGenCells)
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                _cells[x, y] = nextGenCells[x, y];
                _cubes[x, y].GetComponent<MeshRenderer>().enabled = _cells[x, y];
            }
        }
    }
    
    /// <summary>
    /// gather no of neighbors of each cell
    /// </summary>
    /// <returns></returns>
    int GetNumOfNeighbors(int x, int y)
    {
        int neighbors = 0;

        if (_cells[x - 1, y + 1])    // Top Left
            neighbors++;
        if (_cells[x, y + 1])        // Top Middle
            neighbors++;
        if (_cells[x + 1, y + 1])    // Top Right
            neighbors++;
        if (_cells[x + 1, y])        // Middle Right
            neighbors++;
        if (_cells[x + 1, y - 1])    // Bottom Right
            neighbors++;
        if (_cells[x, y - 1])        // Bottom Middle
            neighbors++;
        if (_cells[x - 1, y - 1])    // Bottom Left
            neighbors++;
        if (_cells[x - 1, y])        // Middle Left  
            neighbors++;

        return neighbors;
    }
    
    /// <summary>
    /// check under population
    /// </summary>
    /// <returns></returns>
    bool IsUnderpopulated(int neighbors)
    {
        return neighbors < 2;
    }
    
    /// <summary>
    /// check over population
    /// </summary>
    /// <returns></returns>
    bool IsOverpopulated(int neighbors)
    {
        return neighbors > 3;
    }
    
    /// <summary>
    /// regenerates if cell already in dead state and no of neighbors in next tick is 3
    /// </summary>
    /// <returns></returns> 
    bool CanReproduce(int neighbors, int x, int y)
    {
        return !_cells[x, y] && neighbors == 3;
    }
   
}
