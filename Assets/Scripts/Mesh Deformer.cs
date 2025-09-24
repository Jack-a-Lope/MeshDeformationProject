using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshDeformer : MonoBehaviour
{
    float maxVelocity = 2000f;
    [Range(1f, 1000.0f)]
    public float force = 10f;
    [Range(1f, 1000.0f)]
    public float springForce = 20f;
    [Range(1f, 1000.0f)]
    public float damping = 5f;
    [Range(1f, 1000.0f)]
    public float wiggleForce = 1f;
    Mesh deformingMesh;
    Vector3[] originalVertices, displacedVertices;
    Vector3[] vertexVelocities;
    public float forceOffset = 0.1f;
    [Range(0.01f, 10.0f)]
    public float vertexMass = 1.0f;
    MeshCollider meshCollider;
    MeshFilter meshFilter;
    public bool updateMesh = false;
    public bool wiggle = false;
    [Range(0.01f, 10.0f)]
    public float minWiggleDelay = 1f;
    [Range(0.01f, 10.0f)]
    public float maxWiggleDelay = 2f;
    float wiggleTimer;
    private bool gravity = false;
    [Range(0.01f, 10.0f)]
    private float gravitationalConstant = 1f;
    public bool updateCenter = false;

    //The mesh needs to be significantly large in Blender in order for the distance between vertices to be relevantly large

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Defines the mesh and sets up the lists to keep track of the vertices
        transform.localScale = new Vector3(10, 10, 10);
        deformingMesh = GetComponent<MeshFilter>().mesh;
        originalVertices = deformingMesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
        for (int i = 0; i < originalVertices.Length; i++)
        {
            displacedVertices[i] = originalVertices[i];
        }
        vertexVelocities = new Vector3[originalVertices.Length];
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();
        if (wiggle)
        {
            wiggleTimer = Random.Range(minWiggleDelay, maxWiggleDelay);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Tracks the user input of left clicking
        if (Input.GetMouseButton(0))
        {
            HandleInput();
        }

        if (wiggle)
        {
            wiggleTimer -= Time.deltaTime;
            if (wiggleTimer <= 0)
            {
                HandleWiggle(); 
                wiggleTimer = Random.Range(minWiggleDelay, maxWiggleDelay);
            }
        }

        if (gravity)
        {
            HandleGravity();
        }
    }

    void Update()
    {
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            UpdateVertex(i);
        }
        deformingMesh.vertices = displacedVertices;
        deformingMesh.RecalculateNormals();

        if (updateMesh)
        {
            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }
        if (updateCenter)
        {
            Vector3 total = Vector3.zero;
            for (int i = 0; i < deformingMesh.vertices.Length; i++)
            {
                total += deformingMesh.vertices[i];
            }
            transform.position = (total / deformingMesh.vertices.Length);
            Debug.Log("working");
        }
    }


    void HandleInput()
    {
        //Sends out a ray from the camera to the mouse position
        //If the ray hits the mesh then it handles the deforming forces
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(inputRay, out hit))
        {
            MeshDeformer deformer = hit.collider.GetComponent<MeshDeformer>();
            if (deformer)
            {
                Vector3 point = hit.point;
                point += hit.normal * forceOffset;
                deformer.AddDeformingForce(point, force);
            }
        }
    }

    void HandleWiggle()
    {
        //Selects a random vertex somewhere on the mesh and applies a force orthagonally to it there
        MeshDeformer deformer = GetComponent<MeshDeformer>();
        if (deformer)
        {
            Vector3 point = deformingMesh.vertices[Random.Range(0, deformingMesh.vertices.Length)];
            deformer.AddDeformingForce(point, wiggleForce);
        }
    }

    void HandleGravity()
    {
        float velocity = -(gravitationalConstant) * Time.deltaTime;
        Vector3 velocityY = velocity * new Vector3(0.0f, 1.0f, 0.0f);
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            vertexVelocities[i] += velocityY;
        }

    }

    public void AddDeformingForce(Vector3 point, float force)
    {
        //Draws a line from the camera to the surface in scene view
        //Then for every vertex in displaced vertices, a force is added
        Debug.DrawLine(Camera.main.transform.position, point);
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            AddForceToVertex(i, point, force);
        }
    }

    void AddForceToVertex(int i, Vector3 point, float force)
    {
        //Finds vector between force origin and vertex and then calculates velocity and direction
        Vector3 pointToVertex = displacedVertices[i] - point;
        float attenuatedForce = force / ((1f + pointToVertex.sqrMagnitude));
        float acceleration = attenuatedForce / vertexMass;
        float velocity = acceleration * Time.deltaTime;
        vertexVelocities[i] += pointToVertex.normalized * velocity; //Normalizing gets direction to the velocity's magnitude
        if (vertexVelocities[i].x >= maxVelocity)
        {
            vertexVelocities[i].x = maxVelocity;
        }
        if (vertexVelocities[i].y >= maxVelocity)
        {
            vertexVelocities[i].y = maxVelocity;
        }
        if (vertexVelocities[i].z >= maxVelocity)
        {
            vertexVelocities[i].z = maxVelocity;
        }
    }

    void UpdateVertex(int i)
    {
        //Called by the Update function and handles the actual displacement of the vertices
        Vector3 velocity = vertexVelocities[i];
        Vector3 displacement = displacedVertices[i] - originalVertices[i];
        velocity -= displacement * springForce * Time.deltaTime;
        velocity *= 1f - damping * Time.deltaTime;
        vertexVelocities[i] = velocity;
        displacedVertices[i] += velocity * Time.deltaTime;
    }
}
