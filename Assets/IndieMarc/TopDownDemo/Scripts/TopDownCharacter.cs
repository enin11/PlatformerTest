using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Top down character movement
/// Author: Indie Marc (Marc-Antoine Desbiens)
/// Company: Falling Flames Games
/// </summary>

namespace IndieMarc.TopDown
{
    public class TopDownCharacter : MonoBehaviour
    {
        public int player_id;

        [Header("Movement")]
        public float move_accel = 1f;
        public float move_deccel = 1f;
        public float move_max = 1f;

        [Header("Parts")]
        public GameObject hold_hand;

        [HideInInspector] public UnityAction onDeath;
        
        private Rigidbody2D rigid;
        private Animator animator;
        private AutoOrderLayer auto_order;
        private ContactFilter2D contact_filter;

        private CarryItem carry_item;
        private Vector2 move;
        private Vector2 move_input;
        private Vector2 lookat = Vector2.zero;
        private float side = 1f;
        private bool disable_controls = false;
        private float take_item_timer = 0f;

        private static Dictionary<int, TopDownCharacter> character_list = new Dictionary<int, TopDownCharacter>();

        void Awake()
        {
            character_list[player_id] = this;
            rigid = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            auto_order = GetComponent<AutoOrderLayer>();
        }

        void OnDestroy()
        {
            character_list.Remove(player_id);
        }

        void Start()
        {

        }

        //Handle physics
        void FixedUpdate()
        {
            //Movement velocity
            float desiredSpeedX = Mathf.Abs(move_input.x) > 0.1f ? move_input.x * move_max : 0f;
            float accelerationX = Mathf.Abs(move_input.x) > 0.1f ? move_accel : move_deccel;
            move.x = Mathf.MoveTowards(move.x, desiredSpeedX, accelerationX * Time.fixedDeltaTime);
            float desiredSpeedY = Mathf.Abs(move_input.y) > 0.1f ? move_input.y * move_max : 0f;
            float accelerationY = Mathf.Abs(move_input.y) > 0.1f ? move_accel : move_deccel;
            move.y = Mathf.MoveTowards(move.y, desiredSpeedY, accelerationY * Time.fixedDeltaTime);

            //Move
            rigid.velocity = move;
            
        }

        //Handle render and controls
        void Update()
        {
            move_input = Vector2.zero;

            //Controls
            if (!disable_controls)
            {
                //Controls
                TopDownControls controls = TopDownControls.Get(player_id);
                move_input = controls.GetMove();

                //Items
                take_item_timer += Time.deltaTime;
                if (carry_item && controls.GetActionDown())
                    carry_item.UseItem();
            }

            //Update lookat side
            if (move.magnitude > 0.1f)
                lookat = move.normalized;
            if (Mathf.Abs(lookat.x) > 0.02)
                side = Mathf.Sign(lookat.x);
            
            //Anims
            animator.SetFloat("Speed", move.magnitude);
            animator.SetInteger("Side", GetSideAnim());
            animator.SetBool("Hold", GetHoldingItem() != null);
        }

        private void LateUpdate()
        {
            if (carry_item != null)
                carry_item.UpdateCarryItem();
        }

        public void TakeItem(CarryItem item)
        {
            if (take_item_timer < 0f)
                return;

            if (item.CanTake(gameObject))
            {
                if (!item.HasBearer())
                {
                    //Drop current and take new item
                    DropItem();
                    carry_item = item;
                    item.Take(this);
                    take_item_timer = -0.2f;
                }
            }
        }

        public void DropItem()
        {
            if (carry_item != null)
                carry_item.Drop();
            carry_item = null;
            take_item_timer = -0.2f;
        }

        public CarryItem GetHoldingItem()
        {
            return carry_item;
        }

        public void Kill()
        {
            //To Do
            //Not done because right now there is nothing beyond the demo level.
            //Could make it lose a life, or reload the level
        }

        public void Teleport(Vector3 pos)
        {
            transform.position = pos;
            move = Vector2.zero;
        }

        public Vector3 GetMove()
        {
            return move;
        }

        public int GetSortOrder()
        {
            return auto_order.GetSortOrder();
        }

        //Get Character side
        public float GetSide()
        {
            return side; //Return 1 frame before to let anim do transitions
        }

        public int GetSideAnim()
        {
            return (side >= 0) ? 1 : 3;
        }

        public Vector3 GetHandPos()
        {
            return hold_hand.transform.position;
        }

        public bool IsAlive()
        {
            return true; //TO DO
        }

        public void DisableControls() { disable_controls = true; }
        public void EnableControls() { disable_controls = false; }

        void OnCollisionStay2D(Collision2D coll)
        {
            if (coll.gameObject.GetComponent<Door>() && carry_item && carry_item.GetComponent<Key>())
            {
                carry_item.GetComponent<Key>().TryOpenDoor(coll.gameObject);
            }
        }

        void OnTriggerEnter2D(Collider2D coll)
        {
            if (coll.gameObject.GetComponent<CarryItem>())
            {
                TakeItem(coll.gameObject.GetComponent<CarryItem>());
            }
        }

        public static void LockGameplay()
        {
            foreach (TopDownCharacter character in GetAll())
                character.DisableControls();
        }

        public static void UnlockGameplay()
        {
            foreach (TopDownCharacter character in GetAll())
                character.EnableControls();
        }

        public static TopDownCharacter GetNearest(Vector3 pos, float range = 999f, bool alive_only=true)
        {
            TopDownCharacter nearest = null;
            float min_dist = range;
            foreach (TopDownCharacter character in character_list.Values)
            {
                if (!alive_only || character.IsAlive())
                {
                    float dist = (pos - character.transform.position).magnitude;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = character;
                    }
                }
            }
            return nearest;
        }

        public static TopDownCharacter Get(int player_id)
        {
            foreach (TopDownCharacter character in character_list.Values)
            {
                if (character.player_id == player_id)
                {
                    return character;
                }
            }
            return null;
        }

        public static TopDownCharacter[] GetAll()
        {
            TopDownCharacter[] list = new TopDownCharacter[character_list.Count];
            character_list.Values.CopyTo(list, 0);
            return list;
        }
    }
}
