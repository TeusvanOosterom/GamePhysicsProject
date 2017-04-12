using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Water : MonoBehaviour {

	public ParticleSystem system;
	int max = 800;
	float grav = -9.81f;
	float h = 0.02f; //interaction radius
	//viscosity
	float sigma = 0.3f; //viscosity's linear dependency
	float beta = 3f; //viscosity's quadratic dependency
	//Spring adjustments
	float gamma = 0.08f; //between 0 and 0.2
	float alpha = 0.3f; //plasticity constant (paper: 0.3)
	//Spring displacements
	float kSpring = 0.3f; //spring constant (paper: 0.3)
	//double density relaxation
	float k = 0.08f; //k = stiffness constant (paper: 0.004)
	float kNear = 0.01f; //(paper: 0.01)
	float pZero = 10f; //(paper: 10)

    float waterPosDiffs = 0.9f;
    float shipPosDiffs = 0.1f;

	float CRcoeff = 1f;

	ParticleSystem.Particle[] particles;
	Vector3[] particleVels;
    Vector3[] particleImpulse;
	Vector3[] prevPos;
	float[][] springs;
	List<Vector2> activeSprings = new List<Vector2>();
	int liveParticles;
	List<int>[] neighbourList;

	public GameObject ship;
	float shipMass = 300;
	float shipDrag = 0.01f;
    float shipMov = 0;
	Vector3 shipVel = new Vector3(0, 0, 0);
	List<GameObject> bodies = new List<GameObject>();

	public GameObject ball1;
	int ballsAmount = 1;
	GameObject[] balls;
	bool[] ballsActive;
	Vector3[] ballsVel;
	float ballRadius = 1f;

	bool activated = false;

	void Start () 
	{
		bodies.Add(ship);
		balls = new GameObject[ballsAmount];
		balls[0] = ball1;
		ballsActive = new bool[ballsAmount];
		ballsVel = new Vector3[ballsAmount];
	}
	
	void Update () 
	{
		//Initialize water
		if (Input.GetKeyDown(KeyCode.E) && activated == false)
		{
			system.Emit(max);
			particles = new ParticleSystem.Particle[max];
			particleVels = new Vector3[max];
            particleImpulse = new Vector3[max];
			prevPos = new Vector3[max];
			springs = new float[max][];
			liveParticles = system.GetParticles(particles);
			neighbourList = new List<int>[max];

			for (int i = 0; i < max; i++)
			{
				springs[i] = new float[max];
			}

			activated = true;
		}
		if (Input.GetKey(KeyCode.UpArrow))
		{
			shipMov += 0.025f;
		}
		if (Input.GetKey(KeyCode.DownArrow))
		{
			shipMov -= 0.025f;
		}
		if (Input.GetKey(KeyCode.LeftArrow))
		{
			ship.transform.RotateAround(ship.transform.position, ship.transform.up, -2f);
		}
		if (Input.GetKey(KeyCode.RightArrow))
		{
			ship.transform.RotateAround(ship.transform.position, ship.transform.up, 2f);
		}

		//Fire cannonball, can be repeated
		if (Input.GetKeyDown(KeyCode.Alpha1) && liveParticles > 1 && ballsActive[0] == false)
		{
			ballsActive[0] = true;
			ballsVel[0] = new Vector3(3.6f,2.3f,10f);
			balls[0].transform.position = new Vector3(-5.206f, 0.79f, -10.11f);
		}
		for (int i = 0; i < ballsAmount; i++)
		{
			if(ballsActive[i])
			{
				ballsVel[i] += new Vector3(0, grav, 0) * Time.deltaTime;
				if(balls[i].transform.position.y < -3.5f)
				{
					ballsVel[i] = new Vector3(0, 0, 0);
                    ballsActive[i] = false;
                    balls[i].transform.position -= new Vector3(0, balls[i].transform.position.y + 3.5f, 0);
				}

				if(liveParticles > 0)
				{
					for (int j = 0; j < liveParticles; j++)
					{
						float dist = (particles[j].position - (balls[i].transform.position + ballsVel[i])).sqrMagnitude;
						if (dist < ballRadius)
						{
							particleVels[j] -= new Vector3(0, (ballsVel[i].y - ballRadius-(ballRadius-dist))/1.5f, 0);// *Time.deltaTime;
						}
					}
				}
				balls[i].transform.position += ballsVel[i] * Time.deltaTime;
			}
		}

		//Fountain
		float fountainRadius = 0.5f;
		if (Input.GetKey(KeyCode.F))
		{
			for (int i = 0; i < liveParticles; i++)
			{
				if (particles[i].position.z < fountainRadius && particles[i].position.z > -fountainRadius)
				{
					if (particles[i].position.x < fountainRadius && particles[i].position.x > -fountainRadius)
					{
						if (particles[i].position.y < -2.7f)
						{
							particleVels[i] += new Vector3(0, 20, 0) * Time.deltaTime;
						}
					}
				}
			}
		}
		//Wind/wave, just a constant horizontal force in the x direction
		if (Input.GetKey(KeyCode.G))
		{
			for (int i = 0; i < liveParticles; i++)
			{
				particleVels[i] += new Vector3(1, 0, 0) * Time.deltaTime;
			}
		}

		for (int i = 0; i < liveParticles; i++)
		{
			particleVels[i] += new Vector3(0, grav, 0) * Time.deltaTime;
			particleVels[i] += particleImpulse[i] * Time.deltaTime;
			particleImpulse[i] = Vector3.zero;
		}

		//Get all neighbours per particle
		for (int i = 0; i < liveParticles; i++)
		{
			neighbourList[i] = new List<int>();
			for (int j = i + 1; j < liveParticles; j++)
			{
				if ((particles[i].position - particles[j].position).sqrMagnitude < h)
				{
					neighbourList[i].Add(j);
				}
			}
		}

        //Viscosity
        for (int i = 0; i < liveParticles; i++)
		{
			foreach (int j in neighbourList[i])
			{
				float q = (particles[i].position - particles[j].position).sqrMagnitude / h;
				if(q < 1)
				{
					float u = Vector3.Dot((particleVels[i] - particleVels[j]), (particles[i].position - particles[j].position).normalized);
					if(u > 0)
					{
						Vector3 impulse = Time.deltaTime * (1-q) * (sigma * u + beta * (u*u)) * (particles[i].position - particles[j].position).normalized;
						particleVels[i] -= impulse / 2;
						particleVels[j] += impulse / 2;
					}
				}
			}
		}

		for (int i = 0; i < liveParticles; i++)
		{
			prevPos[i] = particles[i].position;
			particles[i].position += Time.deltaTime * particleVels[i];
		}

		//Adjust springs
		for (int i = 0; i < liveParticles; i++)
		{
			foreach (int j in neighbourList[i])
			{
				float r = (particles[i].position - particles[j].position).sqrMagnitude;
				float q = r / h;
				if(q < 1)
				{
					if (springs[i][j] == 0)
					{
						springs[i][j] = h;
						activeSprings.Add(new Vector2(i,j));
					}

					float d = gamma * springs[i][j];
					if(r > (springs[i][j] + d))
					{
						springs[i][j] = springs[i][j] + Time.deltaTime * alpha * (r - springs[i][j] - d); 
					}
					else if(r < (springs[i][j] - d))
					{
						springs[i][j] = springs[i][j] + Time.deltaTime * alpha * (springs[i][j] - d - r); 
					}
				}
			}
		}
		for (int i = activeSprings.Count-1; i >= 0; i--)
		{
			if(springs[(int)activeSprings[i].x][(int)activeSprings[i].y] > h)
			{
				springs[(int)activeSprings[i].x][(int)activeSprings[i].y] = 0;
				activeSprings.RemoveAt(i);
			}
		}

		//applySpringDisplacements
		foreach(Vector2 spring in activeSprings)
		{
			float L = springs[(int)spring.x][(int)spring.y];
			float r = (particles[(int)spring.x].position - particles[(int)spring.y].position).sqrMagnitude;
			Vector3 rNorm = (particles[(int)spring.x].position - particles[(int)spring.y].position).normalized;
			Vector3 D = (Time.deltaTime*Time.deltaTime) * kSpring * (1 - L / h) * (L - r) * rNorm;
			particles[(int)spring.x].position -= D / 2;
			particles[(int)spring.y].position += D / 2;
		}

		//doubleDensityRelaxation
		for (int i = 0; i < liveParticles; i++)
		{
			float p = 0;
			float pNear = 0;
			foreach(int j in neighbourList[i])
			{
				float r = (particles[i].position - particles[j].position).sqrMagnitude;
				float q = r / h;
				if (q < 1)
				{
					p += Mathf.Pow((1-q), 2);
					pNear += Mathf.Pow((1 - q), 3);
				}
			}
			float P = k * (p - pZero);
			float PNear = kNear * pNear;
			Vector3 dx = Vector3.zero;
			foreach(int j in neighbourList[i])
			{
				float r = (particles[i].position - particles[j].position).sqrMagnitude;
				float q = r / h;
				if(q < 1)
				{
					Vector3 D = (Time.deltaTime * Time.deltaTime) * (P * (1 - q) + PNear * Mathf.Pow((1 - q), 2)) * (particles[i].position - particles[j].position).normalized;
					particles[j].position += D / 2;
					dx -= D / 2;
				}
			}
			particles[i].position += dx;
		}

		//resolveCollisions
		//For now, we only have 1 body, namely the ship
		foreach(GameObject body in bodies)
		{
			//Calculate terminal velocity
			float termVel = Mathf.Sqrt((2 * shipMass * grav) / (1.2f * 10 *0.8f));

			Vector3 origPos = body.transform.position;
			Quaternion OrigOr = body.transform.rotation;
            if (liveParticles > 0)
            {
				//Moving around
				body.transform.position += shipMov * body.transform.forward * Time.deltaTime;
				if (shipMov > 0)
					shipMov -= shipDrag;
				else
					shipMov += shipDrag;
				if (Mathf.Abs(shipMov) < shipDrag)
					shipMov = 0;

				//Gravity
				shipVel += new Vector3(0, grav, 0) * Time.deltaTime;
				if(shipVel.y > termVel)
				{
					shipVel = new Vector3(0, termVel, 0);
				}
				body.transform.position += shipVel * Time.deltaTime;
            }
            FloatBoat(body);

            for (int i = 0; i < liveParticles; i++)
			{
                GameObject boatBase = GameObject.FindGameObjectWithTag("boatBase");
                MeshCollider mesh = boatBase.GetComponent<MeshCollider>();
                if (mesh.bounds.Contains(particles[i].position))
                {
                    float currValue = (particles[i].position - prevPos[i]).magnitude;
                    Vector3 norm = (particles[i].position - prevPos[i]).normalized;
                    particles[i].position += waterPosDiffs * currValue * norm - shipMov * body.transform.forward * Time.deltaTime;
                    particleImpulse[i] += waterPosDiffs * currValue * norm / Time.deltaTime;
                    body.transform.position -= shipPosDiffs * currValue * norm;
                }
            }
            //Make sure the boat is on the water, half the size of a water particle
            if (liveParticles > 0)
				body.transform.position += new Vector3(0, 0.25f, 0);
		}

		

		for (int i = 0; i < liveParticles; i++)
		{
			particleVels[i] = (particles[i].position - prevPos[i]) / Time.deltaTime;
		}

		//Barriers and clear the list of neighbours per particle
        for (int i = 0; i < liveParticles; i++)
		{
			particles[i].position += particleVels[i];
			if (particles[i].position.y < -3.25f)
            {
                float currValue = particles[i].position.y + 3.25f;
                Vector3 posDiffs = currValue * new Vector3(0, -1, 0);
                particles[i].position += posDiffs;
                particleImpulse[i] += CRcoeff * posDiffs / Time.deltaTime;
            }
            if (particles[i].position.x < -2.5f)
            {
                float currValue = particles[i].position.x + 2.5f;
                Vector3 posDiffs = currValue * new Vector3(-1, 0, 0);
                particles[i].position += posDiffs;
                particleImpulse[i] += posDiffs / Time.deltaTime;
            }
            if (particles[i].position.x > 2.5f)
            {
                float currValue = particles[i].position.x - 2.5f;
                Vector3 posDiffs = currValue * new Vector3(-1, 0, 0);
                particles[i].position += posDiffs;
                particleImpulse[i] += posDiffs / Time.deltaTime;
            }
            if (particles[i].position.z < -2.5f)
            {
                float currValue = particles[i].position.z + 2.5f;
                Vector3 posDiffs = currValue * new Vector3(0, 0, -1);
                particles[i].position += posDiffs;
                particleImpulse[i] += posDiffs / Time.deltaTime;
            }
            if (particles[i].position.z > 2.5f)
            {
                float currValue = particles[i].position.z - 2.5f;
                Vector3 posDiffs = currValue * new Vector3(0, 0, -1);
                particles[i].position += posDiffs;
                particleImpulse[i] += posDiffs / Time.deltaTime;
            }
            neighbourList[i].Clear();
		}

		//Update the particle system
        system.SetParticles(particles, liveParticles);
	}

    void FloatBoat(GameObject ship)
    {
        if (ship.transform.position.y < -3.25f && ship.transform.position.x > -2.5f && ship.transform.position.x < 2.5f && ship.transform.position.z > -2.5f && ship.transform.position.z < 2.5f)
        {
            float currValue = ship.transform.position.y + 3.25f;
            Vector3 posDiffs = currValue * new Vector3(0, -1, 0);
            ship.transform.position += posDiffs;
        }
    }
}
