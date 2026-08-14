import { jest } from '@jest/globals'
import { render, screen } from '@testing-library/react'
import { GoogleSignInButton } from './GoogleSignInButton'

/**
 * Jest has no import.meta.env, so `env.googleClientId` reads as empty here. That is
 * the same state as any checkout without a Google project configured, which makes
 * this the unconfigured case rather than an artefact of the test setup.
 */
describe('GoogleSignInButton without a client id', () => {
  it('renders nothing at all', () => {
    const { container } = render(<GoogleSignInButton onCredential={jest.fn()} />)

    // Not a disabled button or an explanatory note: an option the server cannot
    // honour should not be on screen offering itself.
    expect(container).toBeEmptyDOMElement()
    expect(screen.queryByTestId('google-signin')).not.toBeInTheDocument()
  })

  it('does not reach out to Google', () => {
    render(<GoogleSignInButton onCredential={jest.fn()} />)

    // The Identity Services script is only injected once there is a client id to
    // initialise it with, so an unconfigured build makes no third-party request.
    expect(document.querySelector('script[src*="accounts.google.com"]')).toBeNull()
  })
})
