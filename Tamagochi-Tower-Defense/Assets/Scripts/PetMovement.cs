using UnityEngine;
using UnityEngine.InputSystem;

public class PetMovement : MonoBehaviour
{
    public float speed = 3f;
    private Vector2 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        //Detectem el clic del ratolí a la pantalla
        if(Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 touchPos = Camera.main.ScreenToWorldPoint(mousePos);

            //Limitem la posició en l'eix y per evitar que el no tamagotchi surti de l'habitació
            targetPosition = new Vector2(touchPos.x, Mathf.Clamp(touchPos.y, -3.5f, -1f));
            isMoving = true;
        }

        //Si isMoving = true, realitzem el desplaçament
        if(isMoving)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            //Girem el no tamagotchi cap a la direcció del moviment
            if(targetPosition.x < transform.position.x)
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                if (targetPosition.x > transform.position.x)
                {
                    transform.localScale = new Vector3(1, 1, 1);
                }
            }

            //Quan arriba pràcticament a la posició para
            if(Vector2.Distance(transform.position, targetPosition) < 0.05f)
            {
                isMoving = false;
            }
        }
    }
}
