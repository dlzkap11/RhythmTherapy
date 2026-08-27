using UnityEngine;

public class NoteMove : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField ]NoteSpawn ns;

    void Start()
    {
        ns = GetComponentInParent<NoteSpawn>();
    }

    void Update()
    {
        transform.Translate(Vector2.left * speed * Time.deltaTime);


        if(transform.position.x <= -6.78f)
        {
            ns.Release(gameObject);
        }

    }
}
