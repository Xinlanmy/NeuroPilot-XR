# 12.67 XR_HTC_hand_interaction
## Name String
    XR_HTC_hand_interaction
## Revision
    1
## Hand Interaction Profile
### Interaction profile path:
- /interaction_profiles/htc/hand_interaction

### Valid for user paths:
- /user/hand_htc/left
- /user/hand_htc/right

### Supported input source
- ¡K/input/select/value
- ¡K/input/aim/pose

The application should use ¡K/input/aim/pose path to aim at objects in the world and use ¡K/input/select/value path to decide user selection from pinch shape strength which the range of value is 0.0f to 1.0f, with 1.0f meaning pinch fingers touched.

## VIVE Plugin

After adding the "VIVE XR Hand Interaction" to "Project Settings > XR Plugin-in Management > OpenXR > Android Tab > Interaction Profiles", you can use the following Input Action Pathes.

### Left Hand
- <ViveHandInteraction>{LeftHand}/selectValue: Presents the left hand pinch strength.
- <ViveHandInteraction>{LeftHand}/pointerPose: Presents the left hand pinch pose.

### Right Hand
- <ViveHandInteraction>{RightHand}/selectValue: Presents the right hand pinch strength.
- <ViveHandInteraction>{RightHand}/pointerPose: Presents the right hand pinch pose.

Refer to the <VIVE OpenXR sample path>/Samples/Commons/ActionMap/InputActions.inputActions about the "Input Action Path" usage in the sample <VIVE OpenXR sample path>/Samples/Input/OpenXRInput.unity.

--------------------

# 12.31. XR_EXT_hand_interaction
## Name String
    XR_EXT_hand_interaction
## Revision
    1
## Hand Interaction Profile
### Interaction profile path:
- /interaction_profiles/ext/hand_interaction_ext

### Valid for user paths:
- /user/hand/left
- /user/hand/right

### Supported input source
- ¡K/input/aim/pose
- ¡K/input/aim_activate_ext/value: a 1D analog input component indicating that the user activated the action on the target that the user is pointing at with the aim pose.
- ¡K/input/aim_activate_ext/ready_ext: a boolean input, where the value XR_TRUE indicates that the fingers to perform the "aim_activate" gesture are properly tracked by the hand tracking device and the hand shape is observed to be ready to perform or is performing an "aim_activate" gesture.
- ¡K/input/grip/pose
- ¡K/input/grasp_ext/value: a 1D analog input component indicating that the user is making a fist.
- ¡K/input/grasp_ext/ready_ext: a boolean input, where the value XR_TRUE indicates that the hand performing the grasp action is properly tracked by the hand tracking device and it is observed to be ready to perform or is performing the grasp action.
- ¡K/input/pinch_ext/pose
- ¡K/input/pinch_ext/value: a 1D analog input component indicating the extent which the user is bringing their finger and thumb together to perform a "pinch" gesture.
- ¡K/input/pinch_ext/ready_ext: a boolean input, where the value XR_TRUE indicates that the fingers used to perform the "pinch" gesture are properly tracked by the hand tracking device and the hand shape is observed to be ready to perform or is performing a "pinch" gesture.
- ¡K/input/poke_ext/pose

The ¡K/input/aim/pose is typically used for aiming at objects out of arm¡¦s reach. When using a hand interaction profile, it is typically paired with ¡K/input/aim_activate_ext/value to optimize aiming ray stability while performing the gesture. When using a controller interaction profile, the "aim" pose is typically paired with a trigger or a button for aim and fire operations.

The ¡K/input/grip/pose is typically used for holding a large object in the user¡¦s hand. When using a hand interaction profile, it is typically paired with ¡K/input/grasp_ext/value for the user to directly manipulate an object held in a hand. When using a controller interaction profile, the "grip" pose is typically paired with a "squeeze" button or trigger that gives the user the sense of tightly holding an object.

The ¡K/input/pinch_ext/pose is typically used for directly manipulating a small object using the pinch gesture. When using a hand interaction profile, it is typically paired with the ¡K/input/pinch_ext/value gesture. When using a controller interaction profile, it is typically paired with a trigger manipulated with the index finger, which typically requires curling the index finger and applying pressure with the fingertip.

The ¡K/input/poke_ext/pose is typically used for contact-based interactions using the motion of the hand or fingertip. It typically does not pair with other hand gestures or buttons on the controller. The application typically uses a sphere collider with the "poke" pose to visualize the pose and detect touch with a virtual object.

## VIVE Plugin

After adding the "VIVE XR Hand Interaction Ext" to "Project Settings > XR Plugin-in Management > OpenXR > Android Tab > Interaction Profiles", you can use the following Input Action Pathes.

### Left Hand
- <ViveHandInteraction>{LeftHand}/pointerPose: Presents the left hand aim pose used for aiming at objects out of arm¡¦s reach.
- <ViveHandInteraction>{LeftHand}/pointerValue: Can be used as either a boolean or float action type, where the value XR_TRUE or 1.0f represents that the aimed-at target is being fully interacted with left hand.
- <ViveHandInteraction>{LeftHand}/pointerReady: XR_TRUE indicates that the left fingers to perform the "aim_activate" gesture are properly tracked by the hand tracking device and the hand shape is observed to be ready to perform or is performing an "aim_activate" gesture.
- <ViveHandInteraction>{LeftHand}/gripPose: Presents the left hand grip pose used for holding a large object in the user¡¦s hand.
- <ViveHandInteraction>{LeftHand}/gripValue: Can be used as either a boolean or float action type, where the value XR_TRUE or 1.0f represents that the left fist is tightly closed.
- <ViveHandInteraction>{LeftHand}/gripReady: XR_TRUE indicates that the left hand performing the grasp action is properly tracked by the hand tracking device and it is observed to be ready to perform or is performing the grasp action.
- <ViveHandInteraction>{LeftHand}/pinchPose: Presents the left hand pinch pose used for directly manipulating a small object using the pinch gesture.
- <ViveHandInteraction>{LeftHand}/pinchValue: Can be used as either a boolean or float action type, where the value XR_TRUE or 1.0f represents that the left finger and thumb are touching each other.
- <ViveHandInteraction>{LeftHand}/pinchReady: XR_TRUE indicates that the left fingers used to perform the "pinch" gesture are properly tracked by the hand tracking device and the hand shape is observed to be ready to perform or is performing a "pinch" gesture.
- <ViveHandInteraction>{LeftHand}/pokePose: Presents the left hand poke pose used for contact-based interactions using the motion of the hand or fingertip.

### Right Hand
- <ViveHandInteraction>{RightHand}/pointerPose: Presents the right hand aim pose used for aiming at objects out of arm¡¦s reach.
- <ViveHandInteraction>{RightHand}/pointerValue: Can be used as either a boolean or float action type, where the value XR_TRUE or 1.0f represents that the aimed-at target is being fully interacted with right hand.
- <ViveHandInteraction>{RightHand}/pointerReady: XR_TRUE indicates that the right fingers to perform the "aim_activate" gesture are properly tracked by the hand tracking device and the hand shape is observed to be ready to perform or is performing an "aim_activate" gesture.
- <ViveHandInteraction>{RightHand}/gripPose: Presents the right hand grip pose used for holding a large object in the user¡¦s hand.
- <ViveHandInteraction>{RightHand}/gripValue: Can be used as either a boolean or float action type, where the value XR_TRUE or 1.0f represents that the right fist is tightly closed.
- <ViveHandInteraction>{RightHand}/gripReady: XR_TRUE indicates that the right hand performing the grasp action is properly tracked by the hand tracking device and it is observed to be ready to perform or is performing the grasp action.
- <ViveHandInteraction>{RightHand}/pinchPose: Presents the right hand pinch pose used for directly manipulating a small object using the pinch gesture.
- <ViveHandInteraction>{RightHand}/pinchValue: Can be used as either a boolean or float action type, where the value XR_TRUE or 1.0f represents that the right finger and thumb are touching each other.
- <ViveHandInteraction>{RightHand}/pinchReady: XR_TRUE indicates that the right fingers used to perform the "pinch" gesture are properly tracked by the hand tracking device and the hand shape is observed to be ready to perform or is performing a "pinch" gesture.
- <ViveHandInteraction>{RightHand}/pokePose: Presents the right hand poke pose used for contact-based interactions using the motion of the hand or fingertip.

Refer to the <VIVE OpenXR sample path>/Samples/HandInteractionExt/HandInteractionExt.inputActions about the "Input Action Path" usage in the sample <VIVE OpenXR sample path>/Samples/HandInteractionExt/HandInteractionExt.unity.
