using UnityEngine;
using System;
using System.Collections.Generic;

public class TKDragRecognizer : TKAbstractGestureRecognizer
{
	public event Action<TKDragRecognizer> gestureRecognizedEvent;
	public event Action<TKDragRecognizer> gestureCompleteEvent;

	public Vector2 deltaTranslation;
	public float deltaTranslationCm;
	public int minimumNumberOfTouches = 1;
	public int maximumNumberOfTouches = 1;

	private float totalDeltaMovementInCm = 0f;
	private Vector2 _previousLocation;
	private float _minDistanceToDragCm;
	private Vector2 _startPoint;
	private Vector2 _endPoint;
	private Vector2 _currentPoint;

	public Vector2 currentPoint
	{
		get
		{
			return this._currentPoint;
		}
	}
	public Vector2 startPoint
	{
		get
		{
			return this._startPoint;
		}
	}

	public Vector2 endPoint
	{
		get
		{
			return this._endPoint;
		}
	}


	public TKDragRecognizer( float minDragDistanceCm = 0.5f )
	{
		_minDistanceToDragCm = minDragDistanceCm;
	}


	internal override void fireRecognizedEvent()
	{
		if( gestureRecognizedEvent != null )
			gestureRecognizedEvent( this );
	}


	internal override bool touchesBegan( List<TKTouch> touches )
	{
		// extra touches abort gesture
		if( _trackingTouches.Count + touches.Count > maximumNumberOfTouches )
		{
			state = TKGestureRecognizerState.FailedOrEnded;
			return false;
		}

		// add new or additional touches to gesture (allows for two or more touches to be added or removed without ending the pan gesture)
		if( state == TKGestureRecognizerState.Possible || ( ( state == TKGestureRecognizerState.Began || state == TKGestureRecognizerState.RecognizedAndStillRecognizing ) && _trackingTouches.Count < maximumNumberOfTouches ) )
		{
			for( int i = 0; i < touches.Count; i++ )
			{
				// only add touches in the Began phase
				if( touches[i].phase == TouchPhase.Began )
				{
					_trackingTouches.Add( touches[i] );
					_startPoint = touches[0].position;

					if( _trackingTouches.Count == maximumNumberOfTouches )
						break;
				}
			} // end for

			if( _trackingTouches.Count >= minimumNumberOfTouches && _trackingTouches.Count <= maximumNumberOfTouches )
			{
				_previousLocation = touchLocation();
				if( state != TKGestureRecognizerState.RecognizedAndStillRecognizing )
				{
					totalDeltaMovementInCm = 0f;
					state = TKGestureRecognizerState.Began;
				}
			}
		}

		return false;
	}


	internal override void touchesMoved( List<TKTouch> touches )
	{
		//do not engage with touch events if the number of touches is outside our desired constraints
		if( _trackingTouches.Count >= minimumNumberOfTouches && _trackingTouches.Count <= maximumNumberOfTouches )
		{
			_currentPoint = touchLocation();
			deltaTranslation = _currentPoint - _previousLocation;
			deltaTranslationCm = deltaTranslation.magnitude / TouchKit.instance.ScreenPixelsPerCm;
			_previousLocation = _currentPoint;

			if( state == TKGestureRecognizerState.Began )
			{
				totalDeltaMovementInCm += deltaTranslationCm;

				if( Math.Abs( totalDeltaMovementInCm ) >= _minDistanceToDragCm )
				{
					state = TKGestureRecognizerState.RecognizedAndStillRecognizing;
				}
			}
			else
			{
				state = TKGestureRecognizerState.RecognizedAndStillRecognizing;
			}
		}
	}


	internal override void touchesEnded( List<TKTouch> touches )
	{
		_endPoint = touchLocation();

		// remove any completed touches
		for( int i = 0; i < touches.Count; i++ )
		{
			if( touches[i].phase == TouchPhase.Ended )
				_trackingTouches.Remove( touches[i] );
		}

		// if we still have a touch left continue. no touches means its time to reset
		if( _trackingTouches.Count >= minimumNumberOfTouches )
		{
			_previousLocation = touchLocation();
			state = TKGestureRecognizerState.RecognizedAndStillRecognizing;
		}
		else
		{
			// if we had previously been recognizing fire our complete event
			if( state == TKGestureRecognizerState.RecognizedAndStillRecognizing )
			{
				state = TKGestureRecognizerState.Recognized;
				
				if( gestureCompleteEvent != null )
				{
					gestureCompleteEvent( this );
				}
			}

			state = TKGestureRecognizerState.FailedOrEnded;
		}
	}


	public override string ToString()
	{
		return string.Format( "[{0}] state: {1}, location: {2}, deltaTranslation: {3}", this.GetType(), state, touchLocation(), deltaTranslation );
	}

}